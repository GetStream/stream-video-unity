using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Stream.Video.v1.Sfu.Events;
using Stream.Video.v1.Sfu.Models;
using StreamVideo.Core.Auth;
using StreamVideo.Core.Web;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal class SfuWebSocket : BasePersistentWebSocket
    {
        public SfuWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler, IAuthProvider authProvider,
            IRequestUriFactory requestUriFactory, ISerializer serializer, ITimeService timeService, ILogs logs,
            IApplicationInfo applicationInfo, Version sdkVersion)
            : base(websocketClient, reconnectScheduler, authProvider, requestUriFactory, serializer, timeService, logs)
        {
            _applicationInfo = applicationInfo;
            _sdkVersion = sdkVersion;
        }

        public void SetSessionData(string sessionId, string sdpOffer, string sfuUrl, string sfuToken)
        {
            _sfuToken = sfuToken;
            _sfuUrl = sfuUrl;
            _sdpOffer = sdpOffer;
            _sessionId = sessionId;
        }

        protected override void SendHealthCheck()
        {
            var sfuRequest = new SfuRequest
            {
                HealthCheckRequest = new HealthCheckRequest(),
            };

            var sfuRequestByteArray = sfuRequest.ToByteArray();
            WebsocketClient.Send(sfuRequestByteArray);
        }

        protected override async Task OnConnectAsync(CancellationToken cancellationToken = default)
        {
            //StreamTodo: validate session data

            var joinRequest = new JoinRequest
            {
                Token = _sfuToken,
                SessionId = _sessionId,
                SubscriberSdp = _sdpOffer,
                ClientDetails = new ClientDetails
                {
                    Sdk = new Sdk
                    {
                        //StreamTodo: change to Unity once this is merged https://github.com/GetStream/protocol/pull/171
                        Type = SdkType.Angular,
                        Major = _sdkVersion.Major.ToString(),
                        Minor = _sdkVersion.Minor.ToString(),
                        Patch = _sdkVersion.Revision.ToString()
                    },
                    Os = new OS
                    {
                        Name = _applicationInfo.OperatingSystemFamily,
                        Version = _applicationInfo.OperatingSystem,
                        Architecture = _applicationInfo.CpuArchitecture
                    },
                    Device = new Device
                    {
                        Name = _applicationInfo.DeviceName,
                        Version = _applicationInfo.DeviceModel
                    }
                },
            };

            var sfuRequest = new SfuRequest
            {
                JoinRequest = joinRequest,
            };

#if STREAM_DEBUG_ENABLED
            var debugJson = Serializer.Serialize(sfuRequest);
            Logs.Warning(debugJson);
#endif

            var sfuRequestByteArray = sfuRequest.ToByteArray();

            var sfuUri = UriFactory.CreateSfuConnectionUri(_sfuUrl);

            Logs.Info("SFU Connect URI: " + sfuUri);
            await WebsocketClient.ConnectAsync(sfuUri);
            Logs.Info("SFU WS Connected");

            WebsocketClient.Send(sfuRequestByteArray);
        }

        protected override void ProcessMessages()
        {
            while (WebsocketClient.TryDequeueMessage(out var msg))
            {
                var decodedMessage = SfuEvent.Parser.ParseFrom(msg);

#if STREAM_DEBUG_ENABLED
                Logs.Info("WS message: " + decodedMessage);
#endif

                //StreamTodo: handle new message
            }
        }

        protected override string LogsPrefix { get; set; } = "SFU ";

        private readonly Version _sdkVersion;
        private readonly IApplicationInfo _applicationInfo;

        private string _sessionId;
        private string _sdpOffer;
        private string _sfuUrl;
        private string _sfuToken;
    }
}