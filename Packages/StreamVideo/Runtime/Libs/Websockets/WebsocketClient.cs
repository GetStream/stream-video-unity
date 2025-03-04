using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Libs.Websockets
{
    /// <summary>
    /// Implementation of <see cref="IWebsocketClient"/>
    /// </summary>
    public class WebsocketClient : IWebsocketClient
    {
        public event Action Connected;
        public event Action Disconnected;
        public event Action ConnectionFailed;

        public bool IsConnected => State == WebSocketState.Open;
        public bool IsConnecting => State == WebSocketState.Connecting;

        public WebSocketState State => _internalClient?.State ?? WebSocketState.None;

        public int QueuedMessagesCount => _receiveQueue.Count;

        /// <param name="isDebugMode">Additional logs will be printed</param>
        public WebsocketClient(ILogs logs, Encoding encoding = default, bool isDebugMode = false)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _encoding = encoding ?? DefaultEncoding;
            _isDebugMode = isDebugMode;
        }

        public bool TryDequeueMessage(out byte[] message) => _receiveQueue.TryDequeue(out message);

        public async Task ConnectAsync(Uri serverUri)
        {
            if (IsConnected || IsConnecting)
            {
                throw new InvalidOperationException(
                    $"Can't connect during `{State}` state. Please Disconnect() first to cleanup previous connection.");
            }

            _uri = serverUri ?? throw new ArgumentNullException(nameof(serverUri));

            try
            {
                await TryDisposeResourcesAsync(WebSocketCloseStatus.NormalClosure,
                    "Clean up resources before connecting");
                _connectionCts = new CancellationTokenSource();

                _internalClient = new ClientWebSocket();
                await _internalClient.ConnectAsync(_uri, _connectionCts.Token);
            }
            catch (OperationCanceledException e)
            {
                LogExceptionIfDebugMode(e);
                throw;
            }
            catch (WebSocketException e)
            {
                LogExceptionIfDebugMode(e);
                OnConnectionFailed();
                throw;
            }
            catch (SocketException e)
            {
                LogExceptionIfDebugMode(e);
                OnConnectionFailed();
                throw;
            }
            catch (Exception e)
            {
                _logs.Exception(e);
                OnConnectionFailed();
                throw;
            }

            _backgroundSendTimer = new Timer(SendMessagesCallback, null, 0, UpdatePeriod);
            _backgroundReceiveTimer = new Timer(ReceiveMessagesCallback, null, UpdatePeriodOffset, UpdatePeriod);

            Connected?.Invoke();
        }

        public void Send(string message)
        {
            var buffer = _encoding.GetBytes(message);
            var messageSegment = new ArraySegment<byte>(buffer);

            _sendQueue.Add(messageSegment);
        }

        public void Send(byte[] message)
        {
            var messageSegment = new ArraySegment<byte>(message);

            _sendQueue.Add(messageSegment);
        }

        public void Update()
        {
            var disconnect = false;
            while (_threadWebsocketExceptionsLog.TryDequeue(out var webSocketException))
            {
                LogExceptionIfDebugMode(webSocketException);

                disconnect = true;
            }

            if (disconnect)
            {
                DisconnectAsync(WebSocketCloseStatus.ProtocolError, "WebSocket thrown an exception")
                    .ContinueWith(_ => LogExceptionIfDebugMode(_.Exception), TaskContinuationOptions.OnlyOnFaulted);
                return;
            }

            while (_threadExceptionsLog.TryDequeue(out var exception))
            {
                LogExceptionIfDebugMode(exception);
            }
        }

        public async Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage)
        {
            LogInfoIfDebugMode("Disconnect");
            await TryDisposeResourcesAsync(closeStatus, closeMessage);
            
            _receiveQueue.Clear();
            while (_sendQueue.TryTake(out _))
            {
            }

            Disconnected?.Invoke();
        }

        public void Dispose()
        {
            LogInfoIfDebugMode("Dispose");
            DisconnectAsync(WebSocketCloseStatus.NormalClosure, "WebSocket client is disposed")
                .ContinueWith(_ => LogExceptionIfDebugMode(_.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        private const int UpdatesPerSecond = 20;
        private const int UpdatePeriod = 1000 / UpdatesPerSecond;
        private const int UpdatePeriodOffset = UpdatePeriod / 2;

        private static Encoding DefaultEncoding { get; } = Encoding.UTF8;

        private static readonly WebSocketState[] _clientClosedStates = new[]
            { WebSocketState.Closed, WebSocketState.CloseSent, WebSocketState.CloseReceived, WebSocketState.Aborted };

        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<Exception> _threadExceptionsLog = new ConcurrentQueue<Exception>();

        private readonly ConcurrentQueue<WebSocketException> _threadWebsocketExceptionsLog =
            new ConcurrentQueue<WebSocketException>();

        private readonly BlockingCollection<ArraySegment<byte>> _sendQueue =
            new BlockingCollection<ArraySegment<byte>>();

        private readonly ArraySegment<byte> _bufferSegment = new ArraySegment<byte>(new byte[4 * 1024]);

        private readonly ILogs _logs;
        private readonly Encoding _encoding;
        private readonly bool _isDebugMode;

        private readonly SemaphoreSlim _backgroundSendSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _backgroundReceiveSemaphore = new SemaphoreSlim(1);

        private Timer _backgroundSendTimer;
        private Timer _backgroundReceiveTimer;

        private Uri _uri;
        private ClientWebSocket _internalClient;
        private CancellationTokenSource _connectionCts;

        private async void SendMessagesCallback(object state)
        {
            if (!IsConnected || _connectionCts == null || _connectionCts.IsCancellationRequested)
            {
                return;
            }

            if (!_backgroundSendSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                while (_sendQueue.TryTake(out var msg))
                {
                    try
                    {
                        await _internalClient.SendAsync(msg, WebSocketMessageType.Text, true, _connectionCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (WebSocketException webSocketException)
                    {
                        _threadWebsocketExceptionsLog.Enqueue(webSocketException);
                        return;
                    }
                    catch (Exception e)
                    {
                        _threadExceptionsLog.Enqueue(e);
                    }
                }
            }
            finally
            {
                _backgroundSendSemaphore.Release();
            }
        }

        // Runs on a background thread
        private async void ReceiveMessagesCallback(object state)
        {
            if (!IsConnected || _connectionCts == null || _connectionCts.IsCancellationRequested)
            {
                return;
            }

            if (!_backgroundReceiveSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                var result = await TryReceiveSingleMessageAsync();
                if (result != null)
                {
                    _receiveQueue.Enqueue(result);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (WebSocketException webSocketException)
            {
                _threadWebsocketExceptionsLog.Enqueue(webSocketException);
                return;
            }
            catch (Exception e)
            {
                _threadExceptionsLog.Enqueue(e);
            }
            finally
            {
                _backgroundReceiveSemaphore.Release();
            }
        }

        private async Task TryDisposeResourcesAsync(WebSocketCloseStatus closeStatus, string closeMessage)
        {
            try
            {
                if (_backgroundReceiveTimer != null)
                {
                    await _backgroundReceiveTimer.DisposeAsync();
                }

                if (_backgroundSendTimer != null)
                {
                    await _backgroundSendTimer.DisposeAsync();
                }
            }
            catch (Exception e)
            {
                LogExceptionIfDebugMode(e);
            }

            try
            {
                if (_connectionCts != null)
                {
                    _connectionCts.Cancel();
                    _connectionCts.Dispose();
                    _connectionCts = null;
                }
            }
            catch (Exception e)
            {
                LogExceptionIfDebugMode(e);
            }

            if (_internalClient == null)
            {
                return;
            }

            try
            {
                if (!_clientClosedStates.Contains(_internalClient.State))
                {
                    await _internalClient.CloseOutputAsync(closeStatus, closeMessage, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                LogExceptionIfDebugMode(e);
            }
            finally
            {
                //StreamTOdo: this fixes possible null ref if Dispose was called multiple times but perhaps this logic should not be called multiple times
                if (_internalClient == null)
                {
                    _internalClient.Dispose();
                    _internalClient = null;
                }
            }
        }

        private void OnConnectionFailed() => ConnectionFailed?.Invoke();

        // Called from a background thread
        private void OnReceivedCloseMessage()
            => DisconnectAsync(WebSocketCloseStatus.InternalServerError, "Server closed the connection")
                .ContinueWith(_ => LogThreadExceptionIfDebugMode(_.Exception), TaskContinuationOptions.OnlyOnFaulted);

        private async Task<byte[]> TryReceiveSingleMessageAsync()
        {
            using (var ms = new MemoryStream())
            {
                if (_internalClient.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException(
                        "Tried to receive WebSocket message but connection is not Open");
                }

                WebSocketReceiveResult chunkResult;
                do
                {
                    chunkResult = await _internalClient.ReceiveAsync(_bufferSegment, _connectionCts.Token);

                    if (chunkResult.MessageType == WebSocketMessageType.Close)
                    {
                        OnReceivedCloseMessage();
                        return null;
                    }

                    ms.Write(_bufferSegment.Array, _bufferSegment.Offset, chunkResult.Count);
                } while (!chunkResult.EndOfMessage && !_connectionCts.IsCancellationRequested);

                _connectionCts.Token.ThrowIfCancellationRequested();

                //reset position before reading from stream
                ms.Seek(0, SeekOrigin.Begin);

                switch (chunkResult.MessageType)
                {
                    case WebSocketMessageType.Text:
                    case WebSocketMessageType.Binary:
                        return ms.ToArray();
                    case WebSocketMessageType.Close:
                        // Handled above
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (chunkResult.MessageType == WebSocketMessageType.Binary)
                {
                    var exception = new Exception("Unhandled WebSocket message type: " + WebSocketMessageType.Binary);
                    LogThreadExceptionIfDebugMode(exception);
                    throw exception;
                }

                return null;
            }
        }

        private void LogExceptionIfDebugMode(Exception exception)
        {
            if (_isDebugMode)
            {
                _logs.Exception(exception);
            }
        }

        private void LogThreadExceptionIfDebugMode(Exception exception)
        {
            if (_isDebugMode)
            {
                _threadExceptionsLog.Enqueue(exception);
            }
        }

        private void LogInfoIfDebugMode(string info)
        {
            if (_isDebugMode)
            {
                _logs.Info(info);
            }
        }
    }
}