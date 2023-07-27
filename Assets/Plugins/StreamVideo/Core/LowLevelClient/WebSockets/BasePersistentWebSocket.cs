using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.LowLevelClient.Models;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Auth;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
using StreamVideo.Libs.Websockets;

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
                    Connected?.Invoke();
                }
                else if (value == ConnectionState.Disconnected)
                {
                    Disconnected?.Invoke();
                }
            }
        }
        
        public ITokenProvider AuthTokenProvider { get; set; }
        public AuthCredentials AuthCredentials { get; set; }

        //StreamTodo: do we need this?
        // public ReconnectStrategy ReconnectStrategy => _reconnectScheduler.ReconnectStrategy;
        // public float ReconnectConstantInterval => _reconnectScheduler.ReconnectConstantInterval;
        // public float ReconnectExponentialMinInterval => _reconnectScheduler.ReconnectExponentialMinInterval;
        // public float ReconnectExponentialMaxInterval => _reconnectScheduler.ReconnectExponentialMaxInterval;
        // public int ReconnectMaxInstantTrials => _reconnectScheduler.ReconnectMaxInstantTrials;
        public double? NextReconnectTime => _reconnectScheduler.NextReconnectTime;
        
        public void Update()
        {
            TryHandleWebsocketsConnectionFailed();
            TryToReconnect();

            MonitorHealthCheck();

            while (WebsocketClient.TryDequeueMessage(out var msg))
            {
                //StreamTodo: this is different per impl
                var decodedMessage = Encoding.UTF8.GetString(msg);

#if STREAM_DEBUG_ENABLED
                Logs.Info("WS message: " + decodedMessage);
#endif

                HandleNewWebsocketMessage(decodedMessage);
            }
        }

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
            
            OnConnectAsync().LogIfFailed();

            //StreamTodo: is needed? 
            //Reconnecting?.Invoke();

            // if (_tokenProvider != null)
            // {
            //     ConnectUserAsync(_authCredentials.ApiKey, _authCredentials.UserId, _tokenProvider).LogIfFailed();
            // }
            // else
            // {
            //     Connect();
            // }
        }

        public Task ConnectAsync() => OnConnectAsync();

        protected abstract Task OnConnectAsync();
        
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
            Logs.Warning("Websocket connection failed");
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
                PingHealthCheck();
            }

            var timeSinceLastHealthCheck = TimeService.Time - _lastHealthCheckReceivedTime;
            if (timeSinceLastHealthCheck > HealthCheckMaxWaitingTime)
            {
                Logs.Warning($"Health check was not received since: {timeSinceLastHealthCheck}, reset connection");
                WebsocketClient
                    .DisconnectAsync(WebSocketCloseStatus.InternalServerError,
                        $"Health check was not received since: {timeSinceLastHealthCheck}")
                    .ContinueWith(_ => Logs.Exception(_.Exception), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void PingHealthCheck()
        {
            //StreamTodo: handle per implemention
            var healthCheck = new HealthCheckEvent();

            WebsocketClient.Send(Serializer.Serialize(healthCheck));
            _lastHealthCheckSendTime = TimeService.Time;

#if STREAM_DEBUG_ENABLED
            Logs.Info("Health check sent");
#endif
        }
        
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

        //StreamTodo: we should track the persistent WS state like Connecting, Connected, Disconnected, WaitingToReconnect, etc.

        protected BasePersistentWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs)
        {
            UriFactory = requestUriFactory;
            _reconnectScheduler = reconnectScheduler;
            //StreamTodo: assert
            Serializer = serializer;
            TimeService = timeService;
            Logs = logs;
            WebsocketClient = websocketClient ?? throw new ArgumentNullException(nameof(websocketClient));

            WebsocketClient.ConnectionFailed += OnConnectionFailed;
            WebsocketClient.Disconnected += OnDisconnected;
        }

        protected void OnHealthCheckReceived()
        {
            Logs.Info("Health check received");
            _lastHealthCheckReceivedTime = TimeService.Time;
        }

        private const int HealthCheckMaxWaitingTime = 30;

        // For WebGL there is a slight delay when sending therefore we send HC event a bit sooner just in case
        private const int HealthCheckSendInterval = HealthCheckMaxWaitingTime - 1;

        private readonly object _websocketConnectionFailedFlagLock = new object();

        private readonly IReconnectScheduler _reconnectScheduler;
        private readonly StringBuilder _errorSb = new StringBuilder();

        private readonly Dictionary<string, Action<string>> _eventKeyToHandler =
            new Dictionary<string, Action<string>>();

        private ConnectionState _connectionState;

        private bool _websocketConnectionFailed;

        private float _lastHealthCheckReceivedTime;
        private float _lastHealthCheckSendTime;

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
            //ConnectionState = ConnectionState.Disconnected;
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
        
        private void HandleNewWebsocketMessage(string msg)
        {
            const string ErrorKey = "error";

            if (Serializer.TryPeekValue<APIError>(msg, ErrorKey, out var apiError))
            {
                _errorSb.Length = 0;
                apiError.AppendFullLog(_errorSb);

                Logs.Error($"{nameof(APIError)} returned: {_errorSb}");
                return;
            }

            const string TypeKey = "type";

            if (!Serializer.TryPeekValue<string>(msg, TypeKey, out var type))
            {
                Logs.Error($"Failed to find `{TypeKey}` in msg: " + msg);
                return;
            }

            //StreamTodo: do we need debug Event log? 
            var time = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");
            //EventReceived?.Invoke($"{time} - Event received: <b>{type}</b>");

            if (!_eventKeyToHandler.TryGetValue(type, out var handler))
            {
                //StreamTodo: LogLevel should be passed to 
                //if (_config.LogLevel.IsDebugEnabled())
                {
                    Logs.Warning($"No message handler registered for `{type}`. Message not handled: " + msg);
                }

                return;
            }

            handler(msg);
        }
        
        
    }
}