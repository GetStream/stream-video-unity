using System.Threading.Tasks;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal class CoordinatorWebSocket : BasePersistentWebSocket
    {
        public string ConnectionId { get; private set; }
        
        public CoordinatorWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs) 
            : base(websocketClient, reconnectScheduler, requestUriFactory, serializer, timeService, logs)
        {
            RegisterEventType<HealthCheckEvent>(CoordinatorEventType.HealthCheck,
                HandleHealthCheckEvent);

            RegisterEventType<ConnectedEvent>(CoordinatorEventType.ConnectionOk,
                HandleConnectionOkEvent);
        }

        protected override string LogsPrefix { get; set; } = "Coordinator";

        protected override async Task OnConnectAsync()
        {
            //StreamTodo: 1. cancellation token
            //StreamTodo: 2. timeout
            //StreamTodo: 3. multiple attempts (should be covered by reconnect scheduler)
            //StreamTodo: 4. terminate _connectUserTaskSource on errors
            
            _connectUserTaskSource = new TaskCompletionSource<bool>();

            var uri = UriFactory.CreateCoordinatorConnectionUri();

            await WebsocketClient.ConnectAsync(uri);
            
            Logs.Info("WS connected! Let's send the connect message");

            //StreamTodo: handle TokenProvider
            var wsAuthMsg = new WSAuthMessageRequest
            {
                Token = AuthCredentials.UserToken,
                UserDetails = new ConnectUserDetailsRequest
                {
                    //StreamTodo: handle Image & Name
                    Id = AuthCredentials.UserId,
                    //Image = null,
                    //Name = null
                }
            };

            var serializedAuthMsg = Serializer.Serialize(wsAuthMsg);

            WebsocketClient.Send(serializedAuthMsg);
        }

        private TaskCompletionSource<bool> _connectUserTaskSource;

        private void HandleHealthCheckEvent(HealthCheckEvent healthCheckEvent) 
            => OnHealthCheckReceived();

        private void HandleConnectionOkEvent(ConnectedEvent connectedEvent)
        {
            ConnectionId = connectedEvent.ConnectionId;

            ConnectionState = ConnectionState.Connected;

            //StreamTodo: fix this
            //_connectUserTaskSource?.SetResult(connectedEvent.Me);
            _connectUserTaskSource.SetResult(true);

            Logs.Info("Connection confirmed by server with connection id: " + ConnectionId);
        }
    }
}