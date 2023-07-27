using System.Threading.Tasks;
using StreamVideo.Core.Web;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal class SfuWebSocket : BasePersistentWebSocket
    {
        public SfuWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs)
            : base(websocketClient, reconnectScheduler, requestUriFactory, serializer, timeService, logs)
        {
        }

        protected override Task OnConnectAsync()
        {
            throw new System.NotImplementedException();
        }

        protected override string LogsPrefix { get; set; } = "SFU ";
    }
}