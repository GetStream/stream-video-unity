using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Websockets;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;
using WebSocketState = WebSocketSharp.WebSocketState;

namespace Libs.Websockets
{
    public class WebsocketSharpClient : IWebsocketClient
    {
        public WebsocketSharpClient(ILogs logs)
        {
            _logs = logs;
        }
        
        public void Dispose()
        {
            if (_webSocket == null)
            {
                return;
            }

            _webSocket.OnMessage -= OnMessage;

            if (_webSocket.ReadyState != WebSocketState.Closed)
            {
                _webSocket.CloseAsync();
            }

            if (_webSocket is IDisposable disposable)
            {
                disposable?.Dispose();
            }

            _webSocket = null;
        }

        public event Action Connected;
        public event Action Disconnected;
        public event Action ConnectionFailed; //StreamTOdo

        public int QueuedMessagesCount { get; } = 0;

        public bool TryDequeueMessage(out byte[] message)
        {
            if (_messageQueue.TryDequeue(out message))
            {
                return true;
            }

            message = null;
            return false;
        }

        public async Task ConnectAsync(Uri serverUri)
        {
            if (_webSocket != null)
            {
                Dispose();
            }

            try
            {
                _webSocket = new WebSocket(serverUri.ToString());
                _webSocket.ConnectAsync();

                while (_webSocket.ReadyState != WebSocketState.Open)
                {
                    //StreamTodo: implement timeout
                    await Task.Delay(100);
                }

                _webSocket.OnMessage += OnMessage;
                _webSocket.OnClose += OnClose;
                _webSocket.OnError += OnError;
                _webSocket.OnOpen += WebSocketOnOnOpen;

                Connected?.Invoke();
            }
            catch (Exception e)
            {
                _logs.Exception(e);
                ConnectionFailed?.Invoke();
                throw;
            }
        }

        private void WebSocketOnOnOpen(object sender, EventArgs e)
        {

        }

        public void Update()
        {
        }

        public void Send(string message)
        {
            _webSocket.Send(message);
        }

        public void Send(byte[] message)
        {
            _webSocket.SendAsync(message, completed: null);
        }

        public async Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage)
        {
            _webSocket.CloseAsync();
            
            while(_webSocket.ReadyState != WebSocketState.Closed)
            {
                //StreamTodo: implement timeout
                await Task.Delay(100);
            }
        }

        private readonly ConcurrentQueue<byte[]> _messageQueue = new ConcurrentQueue<byte[]>();

        private WebSocket _webSocket;
        private ILogs _logs;

        private void OnError(object sender, ErrorEventArgs e)
        {
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
        }
        
        private void OnMessage(object sender, MessageEventArgs e)
        {
            _messageQueue.Enqueue(e.RawData);
        }
    }
}