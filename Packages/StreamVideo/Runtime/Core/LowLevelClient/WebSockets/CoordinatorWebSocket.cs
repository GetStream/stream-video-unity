using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core.Auth;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.Models;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    /// <summary>
    /// Coordinator WS.
    ///
    /// Connected state is set after we receive the `connection.ok` event from the WS.
    /// So OnConnectAsync is not only establishing a WS connection but also waiting for the event
    /// </summary>
    internal class CoordinatorWebSocket : BasePersistentWebSocket
    {
        public string ConnectionId { get; private set; }
        public OwnUserResponseInternalDTO LocalUserDto { get; private set; }

        public CoordinatorWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IAuthProvider authProvider,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs)
            : base(websocketClient, reconnectScheduler, authProvider, requestUriFactory, serializer, timeService, logs)
        {
            RegisterEventType<HealthCheckEventInternalDTO>(CoordinatorEventType.HealthCheck,
                HandleHealthCheckEvent);

            RegisterEventType<ConnectedEventInternalDTO>(CoordinatorEventType.ConnectionOk,
                HandleConnectedEvent);
        }

        protected override string LogsPrefix { get; set; } = "Coordinator";
        protected override int HealthCheckMaxWaitingTime => 30;
        protected override int HealthCheckSendInterval => 10;

        protected override void ProcessMessages()
        {
            while (WebsocketClient.TryDequeueMessage(out var msg))
            {
                var decodedMessage = Encoding.UTF8.GetString(msg);

#if STREAM_DEBUG_ENABLED
                // Ignoring some messages for causing too much noise in logs
                var ignoredMessages = new[] { "health.check", "audioLevelChanged", "connectionQualityChanged" };
                if(!ignoredMessages.Any(decodedMessage.Contains))
                {
                    Logs.Info($"{LogsPrefix} WS message: " + decodedMessage);
                }
#endif

                HandleNewWebsocketMessage(decodedMessage);
            }
        }

        protected override async Task ExecuteConnectAsync(CancellationToken cancellationToken = default)
        {
            //StreamTodo: 2. timeout
            //StreamTodo: 3. multiple attempts (should be covered by reconnect scheduler)
            //StreamTodo: 4. terminate _connectUserTaskSource on errors

            _connectUserTaskSource = new TaskCompletionSource<bool>(cancellationToken);

            var uri = UriFactory.CreateCoordinatorConnectionUri();

            //StreamTodo: Add cancel token support to WS
            await WebsocketClient.ConnectAsync(uri);

#if STREAM_DEBUG_ENABLED
            Logs.Info("Coordinator connected! Let's send the connect message");
#endif

            //StreamTodo: handle TokenProvider
            var authMessage = new WSAuthMessageInternalDTO()
            {
                Token = AuthProvider.UserToken,
                UserDetails = new ConnectUserDetailsRequestInternalDTO
                {
                    //StreamTodo: handle Image & Name
                    Id = AuthProvider.UserId,
                    //Image = null,
                    //Name = null
                }
            };

            var serializedAuthMsg = Serializer.Serialize(authMessage);

#if STREAM_DEBUG_ENABLED
            Logs.Info($"Coordinator auth message: {serializedAuthMsg}");
#endif
            
            WebsocketClient.Send(serializedAuthMsg);

            await _connectUserTaskSource.Task;
        }

        protected override void OnDisconnecting()
        {
            _connectUserTaskSource?.TrySetCanceled();
            
            base.OnDisconnecting();
        }

        protected override void OnDisposing()
        {
            _connectUserTaskSource?.TrySetCanceled();
            
            base.OnDisposing();
        }

        protected override void SendHealthCheck()
        {
            WebsocketClient.Send(Serializer.Serialize(new HealthCheckEventInternalDTO()));
        }

        private readonly StringBuilder _errorSb = new StringBuilder();
        private TaskCompletionSource<bool> _connectUserTaskSource;

        private void HandleHealthCheckEvent(HealthCheckEventInternalDTO healthCheckEvent) => OnHealthCheckReceived();

        private void HandleConnectedEvent(ConnectedEventInternalDTO connectedEvent)
        {
            ConnectionId = connectedEvent.ConnectionId;

            ConnectionState = ConnectionState.Connected;

            LocalUserDto = connectedEvent.Me;
            
            _connectUserTaskSource.SetResult(true);
            _connectUserTaskSource = null;

#if STREAM_DEBUG_ENABLED
            Logs.Info("Connected to the coordinator. Connection id: " + ConnectionId);
#endif
        }

        private void HandleNewWebsocketMessage(string msg)
        {
            const string errorKey = "error";
            const string typeKey = "type";

            if (Serializer.TryPeekValue<APIError>(msg, errorKey, out var apiError))
            {
                _errorSb.Length = 0;
                apiError.AppendFullLog(_errorSb);

                Logs.Error($"{nameof(APIError)} returned: {_errorSb}");
                return;
            }

            if (!Serializer.TryPeekValue<string>(msg, typeKey, out var type))
            {
                Logs.Error($"Failed to find `{typeKey}` in msg: " + msg);
                return;
            }

            if (!EventHandlers.TryGetValue(type, out var handler))
            {
#if STREAM_DEBUG_ENABLED
                Logs.Warning($"No message handler registered for `{type}`. Message not handled: " + msg);
#endif
                return;
            }

            handler(msg);
        }
    }
}