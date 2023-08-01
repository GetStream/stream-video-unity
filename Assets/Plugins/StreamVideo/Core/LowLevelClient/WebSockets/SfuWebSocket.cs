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
using Error = Stream.Video.v1.Sfu.Events.Error;
using ICETrickle = Stream.Video.v1.Sfu.Models.ICETrickle;

namespace StreamVideo.Core.LowLevelClient.WebSockets
{
    internal class SfuWebSocket : BasePersistentWebSocket
    {
        public event Action<SubscriberOffer> SubscriberOffer;
        public event Action<PublisherAnswer> PublisherAnswer;
        public event Action<ConnectionQualityChanged> ConnectionQualityChanged;
        public event Action<AudioLevelChanged> AudioLevelChanged;
        public event Action<ICETrickle> IceTrickle;
        public event Action<ChangePublishQuality> ChangePublishQuality;
        public event Action<ParticipantJoined> ParticipantJoined;
        public event Action<ParticipantLeft> ParticipantLeft;
        public event Action<DominantSpeakerChanged> DominantSpeakerChanged;
        public event Action<JoinResponse> JoinResponse;
        public event Action<HealthCheckResponse> HealthCheckResponse;
        public event Action<TrackPublished> TrackPublished;
        public event Action<TrackUnpublished> TrackUnpublished;
        public event Action<Error> Error;
        public event Action<CallGrantsUpdated> CallGrantsUpdated;
        public event Action<GoAway> GoAway;
        
        public SfuWebSocket(IWebsocketClient websocketClient, IReconnectScheduler reconnectScheduler,
            IAuthProvider authProvider,
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

            Logs.Info($"{LogsPrefix} Connect URI: " + sfuUri);
            await WebsocketClient.ConnectAsync(sfuUri);
            Logs.Info($"{LogsPrefix} WS Connected");

            WebsocketClient.Send(sfuRequestByteArray);
        }

        protected override void ProcessMessages()
        {
            while (WebsocketClient.TryDequeueMessage(out var msg))
            {
                var sfuEvent = SfuEvent.Parser.ParseFrom(msg);

#if STREAM_DEBUG_ENABLED
                Logs.Info($"{LogsPrefix} WS message: " + sfuEvent);
#endif

                switch (sfuEvent.EventPayloadCase)
                {
                    case SfuEvent.EventPayloadOneofCase.None:
                        break;
                    case SfuEvent.EventPayloadOneofCase.SubscriberOffer:
                        SubscriberOffer?.Invoke(sfuEvent.SubscriberOffer);
                        break;
                    case SfuEvent.EventPayloadOneofCase.PublisherAnswer:
                        PublisherAnswer?.Invoke(sfuEvent.PublisherAnswer);
                        break;
                    case SfuEvent.EventPayloadOneofCase.ConnectionQualityChanged:
                        ConnectionQualityChanged?.Invoke(sfuEvent.ConnectionQualityChanged);
                        break;
                    case SfuEvent.EventPayloadOneofCase.AudioLevelChanged:
                        AudioLevelChanged?.Invoke(sfuEvent.AudioLevelChanged);
                        break;
                    case SfuEvent.EventPayloadOneofCase.IceTrickle:
                        IceTrickle?.Invoke(sfuEvent.IceTrickle);
                        break;
                    case SfuEvent.EventPayloadOneofCase.ChangePublishQuality:
                        ChangePublishQuality?.Invoke(sfuEvent.ChangePublishQuality);
                        break;
                    case SfuEvent.EventPayloadOneofCase.ParticipantJoined:
                        ParticipantJoined?.Invoke(sfuEvent.ParticipantJoined);
                        break;
                    case SfuEvent.EventPayloadOneofCase.ParticipantLeft:
                        ParticipantLeft?.Invoke(sfuEvent.ParticipantLeft);
                        break;
                    case SfuEvent.EventPayloadOneofCase.DominantSpeakerChanged:
                        DominantSpeakerChanged?.Invoke(sfuEvent.DominantSpeakerChanged);
                        break;
                    case SfuEvent.EventPayloadOneofCase.JoinResponse:
                        JoinResponse?.Invoke(sfuEvent.JoinResponse);
                        break;
                    case SfuEvent.EventPayloadOneofCase.HealthCheckResponse:
                        HealthCheckResponse?.Invoke(sfuEvent.HealthCheckResponse);
                        break;
                    case SfuEvent.EventPayloadOneofCase.TrackPublished:
                        TrackPublished?.Invoke(sfuEvent.TrackPublished);
                        break;
                    case SfuEvent.EventPayloadOneofCase.TrackUnpublished:
                        TrackUnpublished?.Invoke(sfuEvent.TrackUnpublished);
                        break;
                    case SfuEvent.EventPayloadOneofCase.Error:
                        Error?.Invoke(sfuEvent.Error);
                        break;
                    case SfuEvent.EventPayloadOneofCase.CallGrantsUpdated:
                        CallGrantsUpdated?.Invoke(sfuEvent.CallGrantsUpdated);
                        break;
                    case SfuEvent.EventPayloadOneofCase.GoAway:
                        GoAway?.Invoke(sfuEvent.GoAway);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(sfuEvent.EventPayloadCase),
                            sfuEvent.EventPayloadCase, null);
                }
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