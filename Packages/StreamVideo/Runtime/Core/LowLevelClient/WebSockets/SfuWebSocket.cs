using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using StreamVideo.v1.Sfu.Events;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.Core.Auth;
using StreamVideo.Core.Web;
using StreamVideo.Libs.AppInfo;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Websockets;
using Error = StreamVideo.v1.Sfu.Events.Error;
using ICETrickle = StreamVideo.v1.Sfu.Models.ICETrickle;

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
        public event Action<TrackPublished> TrackPublished;
        public event Action<TrackUnpublished> TrackUnpublished;
        public event Action<Error> Error;
        public event Action<CallGrantsUpdated> CallGrantsUpdated;
        public event Action<GoAway> GoAway;
        public event Action<ICERestart> IceRestart;
        public event Action<PinsChanged> PinsUpdated;
        public event Action CallEnded;
        public event Action<ParticipantUpdated> ParticipantUpdated;
        public event Action ParticipantMigrationComplete;
        public event Action<ChangePublishOptions> ChangePublishOptions;

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
            
#if STREAM_DEBUG_ENABLED
            Logs.Info($"[SFU WS] SetSessionData: sessionId: {_sessionId}, sdpOffer: {_sdpOffer}, sfuUrl: {_sfuUrl}, sfuToken: {_sfuToken}");
#endif
        }

        public void SendLeaveCallRequest(string reason = "")
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                throw new ArgumentException($"{nameof(_sessionId)} is null or empty.");
            }

            if (reason == null)
            {
                throw new ArgumentException($"{nameof(reason)} is null.");
            }
            
            var sfuRequest = new SfuRequest
            {
                LeaveCallRequest = new LeaveCallRequest
                {
                    SessionId = _sessionId,
                    Reason = reason
                }
            };

            var sfuRequestByteArray = sfuRequest.ToByteArray();
            WebsocketClient.Send(sfuRequestByteArray);
        }

        protected override string LogsPrefix { get; set; } = "[SFU WS]";

        protected override int HealthCheckMaxWaitingTime => 30;
        protected override int HealthCheckSendInterval => 10;

        protected override void SendHealthCheck()
        {
            var sfuRequest = new SfuRequest
            {
                HealthCheckRequest = new HealthCheckRequest(),
            };

            var sfuRequestByteArray = sfuRequest.ToByteArray();
            WebsocketClient.Send(sfuRequestByteArray);
        }

        protected override async Task ExecuteConnectAsync(CancellationToken cancellationToken = default)
        {
            //StreamTodo: validate session data

            if (string.IsNullOrEmpty(_sfuToken))
            {
                throw new ArgumentException($"{nameof(_sfuToken)} is null or empty");
            }

            _connectUserTaskSource = new TaskCompletionSource<bool>(cancellationToken);

            try
            {
                //StreamTOdo: implement missing fields: PublisherSdp, ReconnectDetails
                var joinRequest = new JoinRequest
                {
                    Token = _sfuToken,
                    SessionId = _sessionId,
                    SubscriberSdp = _sdpOffer,
                    ClientDetails = new ClientDetails
                    {
                        Sdk = new Sdk
                        {
                            Type = SdkType.Unity,
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

                var sfuJoinRequest = new SfuRequest
                {
                    JoinRequest = joinRequest,
                };

#if STREAM_DEBUG_ENABLED
                var debugJson = Serializer.Serialize(sfuJoinRequest);
                Logs.Warning(debugJson);
#endif

                var sfuJoinRequestEncoded = sfuJoinRequest.ToByteArray();

                var sfuUri = UriFactory.CreateSfuConnectionUri(_sfuUrl);

                await WebsocketClient.ConnectAsync(sfuUri);

                //StreamTodo: review when is the actual "connected state" - perhaps not the WS connection itself but receiving an appropriate event should set the flag
                //e.g. are we able to send any data as soon as the connection is established?

                WebsocketClient.Send(sfuJoinRequestEncoded);

                await _connectUserTaskSource.Task;
            }
            catch (Exception e)
            {
                if (!_connectUserTaskSource.TrySetException(e))
                {
                    Logs.Error($"Failed set exception in {nameof(_connectUserTaskSource)}. Exception:" + e.Message);
                }

                throw;
            }
        }

        protected override void ProcessMessages()
        {
            while (WebsocketClient.TryDequeueMessage(out var msg))
            {
                var sfuEvent = SfuEvent.Parser.ParseFrom(msg);

#if STREAM_DEBUG_ENABLED
                DebugLogEvent(sfuEvent, msg);
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
                        OnHandleJoinResponse(sfuEvent.JoinResponse);
                        break;
                    case SfuEvent.EventPayloadOneofCase.HealthCheckResponse:
                        //StreamTodo: healthCheck contains participantsCount, we should probably sync our state with this info
                        OnHealthCheckReceived();
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
                    case SfuEvent.EventPayloadOneofCase.IceRestart:
                        IceRestart?.Invoke(sfuEvent.IceRestart);
                        break;
                    case SfuEvent.EventPayloadOneofCase.PinsUpdated:
                        PinsUpdated?.Invoke(sfuEvent.PinsUpdated);
                        break;
                    case SfuEvent.EventPayloadOneofCase.CallEnded:
                        CallEnded?.Invoke();
                        break;
                    case SfuEvent.EventPayloadOneofCase.ParticipantUpdated:
                        ParticipantUpdated?.Invoke(sfuEvent.ParticipantUpdated);
                        break;
                    case SfuEvent.EventPayloadOneofCase.ParticipantMigrationComplete:
                        ParticipantMigrationComplete?.Invoke();
                        break;
                    case SfuEvent.EventPayloadOneofCase.ChangePublishOptions:
                        ChangePublishOptions?.Invoke(sfuEvent.ChangePublishOptions);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(sfuEvent.EventPayloadCase),
                            sfuEvent.EventPayloadCase, null);
                }
            }
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

        private readonly Version _sdkVersion;
        private readonly IApplicationInfo _applicationInfo;

        private string _sessionId;
        private string _sdpOffer;
        private string _sfuUrl;
        private string _sfuToken;

        private TaskCompletionSource<bool> _connectUserTaskSource;

        private void OnHandleJoinResponse(JoinResponse joinResponse)
        {
            ConnectionState = ConnectionState.Connected;

            _connectUserTaskSource.SetResult(true);
            _connectUserTaskSource = null;

            JoinResponse?.Invoke(joinResponse);
        }

#if STREAM_DEBUG_ENABLED
        private bool IsEventSubscribedTo(SfuEvent.EventPayloadOneofCase tag)
        {
            switch (tag)
            {
                case SfuEvent.EventPayloadOneofCase.None:
                    return false;
                case SfuEvent.EventPayloadOneofCase.SubscriberOffer:
                    return SubscriberOffer != null;

                case SfuEvent.EventPayloadOneofCase.PublisherAnswer:
                    return PublisherAnswer != null;

                case SfuEvent.EventPayloadOneofCase.ConnectionQualityChanged:
                    return ConnectionQualityChanged != null;

                case SfuEvent.EventPayloadOneofCase.AudioLevelChanged:
                    return AudioLevelChanged != null;

                case SfuEvent.EventPayloadOneofCase.IceTrickle:
                    return IceTrickle != null;

                case SfuEvent.EventPayloadOneofCase.ChangePublishQuality:
                    return ChangePublishQuality != null;

                case SfuEvent.EventPayloadOneofCase.ParticipantJoined:
                    return ParticipantJoined != null;

                case SfuEvent.EventPayloadOneofCase.ParticipantLeft:
                    return ParticipantLeft != null;

                case SfuEvent.EventPayloadOneofCase.DominantSpeakerChanged:
                    return DominantSpeakerChanged != null;

                case SfuEvent.EventPayloadOneofCase.JoinResponse:
                    return JoinResponse != null;

                case SfuEvent.EventPayloadOneofCase.HealthCheckResponse:
                    return true; // Handled internally

                case SfuEvent.EventPayloadOneofCase.TrackPublished:
                    return TrackPublished != null;

                case SfuEvent.EventPayloadOneofCase.TrackUnpublished:
                    return TrackUnpublished != null;

                case SfuEvent.EventPayloadOneofCase.Error:
                    return Error != null;

                case SfuEvent.EventPayloadOneofCase.CallGrantsUpdated:
                    return CallGrantsUpdated != null;

                case SfuEvent.EventPayloadOneofCase.GoAway:
                    return GoAway != null;

                case SfuEvent.EventPayloadOneofCase.IceRestart:
                    return IceRestart != null;

                case SfuEvent.EventPayloadOneofCase.PinsUpdated:
                    return PinsUpdated != null;
                
                case SfuEvent.EventPayloadOneofCase.CallEnded:
                   return CallEnded != null;
                
                case SfuEvent.EventPayloadOneofCase.ParticipantUpdated:
                    return ParticipantUpdated != null;
                
                case SfuEvent.EventPayloadOneofCase.ParticipantMigrationComplete:
                    return ParticipantMigrationComplete != null;
                
                case SfuEvent.EventPayloadOneofCase.ChangePublishOptions:
                    return ChangePublishOptions != null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tag),
                        tag, null);
            }
        }

        private void DebugLogEvent(SfuEvent sfuEvent, byte[] rawMessage)
        {
            if (sfuEvent.EventPayloadCase == SfuEvent.EventPayloadOneofCase.HealthCheckResponse)
            {
                return;
            }

            if (!IsEventSubscribedTo(sfuEvent.EventPayloadCase))
            {
                Logs.Warning($"-----------------------{LogsPrefix} UNHANDLED WS message: " + sfuEvent);
                return;
            }
            
            var decodedMessage = System.Text.Encoding.UTF8.GetString(rawMessage);
            
            // Ignoring some messages for causing too much noise in logs
            var ignoredMessages = new[] { "health.check", "audioLevelChanged", "connectionQualityChanged" };
            if(ignoredMessages.Any(m => decodedMessage.IndexOf(m, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return;
            }

            Logs.Info($"{LogsPrefix} WS message: " + sfuEvent);
        }
#endif
    }
}