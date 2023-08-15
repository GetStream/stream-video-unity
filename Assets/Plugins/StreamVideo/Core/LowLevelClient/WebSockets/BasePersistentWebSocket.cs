using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core.Auth;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
using StreamVideo.Libs.Websockets;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal abstract class BasePersistentWebSocket : IPersistentWebSocket
    {
        public event ConnectionStateChangeHandler ConnectionStateChanged;
        public event Action Connected;
        public event Action Disconnected;

        public ConnectionState ConnectionState
        {
            get => _connectionState;
            protected set
            {
                if (_connectionState == value)
                {
                    return;
                }

                var previous = _connectionState;
                _connectionState = value;
                ConnectionStateChanged?.Invoke(previous, _connectionState);

                if (value == ConnectionState.Connected)
                {
                    OnConnected();
                    Connected?.Invoke();
                }
                else if (value == ConnectionState.Disconnected)
                {
                    Disconnected?.Invoke();
                }
            }
        }

        public double? NextReconnectTime => _reconnectScheduler.NextReconnectTime;

        public void Update()
        {
            TryHandleWebsocketsConnectionFailed();
            TryToReconnect();

            MonitorHealthCheck();

            ProcessMessages();
        }

        protected abstract void ProcessMessages();

        private void TryToReconnect()
        {
            if (!ConnectionState.IsValidToConnect() || !NextReconnectTime.HasValue)
            {
                return;
            }

            if (NextReconnectTime.Value > TimeService.Time)
            {
                return;
            }

            ConnectAsync().LogIfFailed();
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            //StreamTodo: TryCancelWaitingForUserConnection()
            ConnectionState = ConnectionState.Connecting;
            try
            {
                return OnConnectAsync(cancellationToken);
            }
            catch (Exception e)
            {
                ConnectionState = ConnectionState.Disconnected;
                throw;
            }
        }

        public Task DisconnectAsync(WebSocketCloseStatus closeStatus, string closeMessage)
        {
            //StreamTodo: ignore if already disconnected or disconnecting
            OnDisconnecting();

            if (WebsocketClient == null)
            {
                return Task.CompletedTask;
            }

            return WebsocketClient.DisconnectAsync(closeStatus, closeMessage);
        }

        //StreamTodo: either move to coordinator or make generic and pass TMessageType and abstract deserializer
        public void RegisterEventType<TDto, TEvent>(string key,
            Action<TEvent, TDto> handler, Action<TDto> internalHandler = null)
            where TEvent : ILoadableFrom<TDto, TEvent>, new()
        {
            if (_eventKeyToHandler.ContainsKey(key))
            {
                Logs.Warning($"Event handler with key `{key}` is already registered. Ignored");
                return;
            }

            _eventKeyToHandler.Add(key, serializedContent =>
            {
                try
                {
                    var eventObj = DeserializeEvent<TDto, TEvent>(serializedContent, out var dto);
                    handler?.Invoke(eventObj, dto);
                    internalHandler?.Invoke(dto);
                }
                catch (Exception e)
                {
                    Logs.Exception(e);
                }
            });
        }

        public void RegisterEventType<TDto>(string key,
            Action<TDto> internalHandler = null)
        {
            if (_eventKeyToHandler.ContainsKey(key))
            {
                Logs.Warning($"Event handler with key `{key}` is already registered. Ignored");
                return;
            }

            _eventKeyToHandler.Add(key, serializedContent =>
            {
                try
                {
                    var dto = Serializer.Deserialize<TDto>(serializedContent);
                    internalHandler?.Invoke(dto);
                }
                catch (Exception e)
                {
                    Logs.Exception(e);
                }
            });
        }

        public void Dispose()
        {
            //StreamTodo: Cancel waiting for connected task
            ConnectionState = ConnectionState.Closing;
            OnDisposing();

            WebsocketClient.ConnectionFailed -= OnConnectionFailed;
            WebsocketClient.Disconnected -= OnDisconnected;

            //StreamTodo: we're disposing the WS but we're not the owner, would be better to accept a factory method so we own the obj
            WebsocketClient.Dispose();
        }

        protected IWebsocketClient WebsocketClient { get; private set; }
        protected abstract string LogsPrefix { get; set; }
        protected ILogs Logs { get; }
        protected ITimeService TimeService { get; }
        protected ISerializer Serializer { get; }
        protected IRequestUriFactory UriFactory { get; }
        protected IAuthProvider AuthProvider { get; }
        protected IReadOnlyDictionary<string, Action<string>> EventHandlers => _eventKeyToHandler;
        
        protected abstract int HealthCheckMaxWaitingTime { get; }
        protected abstract int HealthCheckSendInterval { get; }

        protected BasePersistentWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IAuthProvider authProvider,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs)
        {
            AuthProvider = authProvider;
            //StreamTodo: assert
            UriFactory = requestUriFactory;
            _reconnectScheduler = reconnectScheduler;
            Serializer = serializer;
            TimeService = timeService;
            Logs = logs;
            WebsocketClient = websocketClient ?? throw new ArgumentNullException(nameof(websocketClient));

            WebsocketClient.ConnectionFailed += OnConnectionFailed;
            WebsocketClient.Disconnected += OnDisconnected;
            
            _reconnectScheduler.ReconnectionScheduled += OnReconnectionScheduled;
        }

        protected abstract void SendHealthCheck();

        protected abstract Task OnConnectAsync(CancellationToken cancellationToken = default);

        protected void OnHealthCheckReceived()
        {
            
#if STREAM_DEBUG_ENABLED
            var timeSinceLast = Mathf.Round(TimeService.Time - _lastHealthCheckReceivedTime);
            Logs.Info($"{LogsPrefix} Health check RECEIVED. Time since last: {timeSinceLast} seconds");
#endif
            _lastHealthCheckReceivedTime = TimeService.Time;
        }

        protected virtual void OnDisconnecting()
        {
        }

        protected virtual void OnDisposing()
        {
        }

        private readonly object _websocketConnectionFailedFlagLock = new object();
        private readonly IReconnectScheduler _reconnectScheduler;
        private readonly Dictionary<string, Action<string>> _eventKeyToHandler =
            new Dictionary<string, Action<string>>();
        private readonly StringBuilder _logSb = new StringBuilder();

        private ConnectionState _connectionState;
        private bool _websocketConnectionFailed;
        private float _lastHealthCheckReceivedTime;
        private float _lastHealthCheckSendTime;

        private void TryHandleWebsocketsConnectionFailed()
        {
            lock (_websocketConnectionFailedFlagLock)
            {
                if (!_websocketConnectionFailed)
                {
                    return;
                }

                _websocketConnectionFailed = false;
            }

#if STREAM_DEBUG_ENABLED
            Logs.Warning($"{LogsPrefix} Websocket connection failed");
#endif

            ConnectionState = ConnectionState.Disconnected;
        }

        private void MonitorHealthCheck()
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            var timeSinceLastHealthCheckSent = TimeService.Time - _lastHealthCheckSendTime;
            if (timeSinceLastHealthCheckSent > HealthCheckSendInterval)
            {
                SendHealthCheck();
                _lastHealthCheckSendTime = TimeService.Time;

#if STREAM_DEBUG_ENABLED
                Logs.Info($"{LogsPrefix} Health check SENT. Time since last: {Mathf.Round(timeSinceLastHealthCheckSent)} seconds");
#endif
            }

            var timeSinceLastHealthCheck = TimeService.Time - _lastHealthCheckReceivedTime;
            if (timeSinceLastHealthCheck > HealthCheckMaxWaitingTime)
            {
                Logs.Warning($"{LogsPrefix} Health check was not received since: {timeSinceLastHealthCheck}, reset connection");
                WebsocketClient
                    .DisconnectAsync(WebSocketCloseStatus.InternalServerError,
                        $"{LogsPrefix} Health check was not received since: {timeSinceLastHealthCheck}")
                    .ContinueWith(_ => Logs.Exception(_.Exception), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// This event can be called by a background thread and we must propagate it on the main thread
        /// Otherwise any call to Unity API would result in Exception. Unity API can only be called from the main thread
        /// </summary>
        private void OnConnectionFailed()
        {
            lock (_websocketConnectionFailedFlagLock)
            {
                _websocketConnectionFailed = true;
            }
        }

        private void OnDisconnected()
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"{LogsPrefix} Websocket Disconnected");
#endif
            ConnectionState = ConnectionState.Disconnected;
        }

        private void OnConnected()
        {
            _lastHealthCheckReceivedTime = TimeService.Time;
        }

        private TEvent DeserializeEvent<TDto, TEvent>(string content, out TDto dto)
            where TEvent : ILoadableFrom<TDto, TEvent>, new()
        {
            try
            {
                dto = Serializer.Deserialize<TDto>(content);
            }
            catch (Exception e)
            {
                throw new StreamDeserializationException(content, typeof(TDto), e);
            }

            var response = new TEvent();
            response.LoadFromDto(dto);

            return response;
        }

        private void OnReconnectionScheduled()
        {
            ConnectionState = ConnectionState.WaitToReconnect;
            var timeLeft = NextReconnectTime.Value - TimeService.Time;

            _logSb.Append("Reconnect scheduled to time: <b>");
            _logSb.Append(Math.Round(NextReconnectTime.Value));
            _logSb.Append(" seconds</b>, current time: <b>");
            _logSb.Append(Math.Round(TimeService.Time));
            _logSb.Append(" seconds</b>, time left: <b>");
            _logSb.Append(Math.Round(timeLeft));
            _logSb.Append(" seconds</b>");

            Logs.Info(_logSb.ToString());
            _logSb.Clear();
        }
    }
}