#if UNITY_ANDROID && !UNITY_EDITOR
#define STREAM_NATIVE_AUDIO //Defined in multiple files
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StreamVideo.v1.Sfu.Events;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using StreamVideo.Core.Configs;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.LowLevelClient.WebSockets;
using StreamVideo.Core.Models;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Time;
using StreamVideo.Libs.Utils;
using Unity.WebRTC;
using UnityEngine;
using SfuError = StreamVideo.v1.Sfu.Events.Error;
using SfuICETrickle = StreamVideo.v1.Sfu.Models.ICETrickle;
using TrackType = StreamVideo.Core.Models.Sfu.TrackType;
using SfuTrackType = StreamVideo.v1.Sfu.Models.TrackType;
using StreamVideo.Core.Sfu;
using StreamVideo.Core.Stats;

namespace StreamVideo.Core.LowLevelClient
{
    public delegate void ParticipantTrackChangedHandler(IStreamVideoCallParticipant participant, IStreamTrack track);

    public delegate void ParticipantJoinedHandler(IStreamVideoCallParticipant participant);

    public delegate void ParticipantLeftHandler(string sessionId, string userId);

    //StreamTodo: Implement GeneratedApi.UpdateMuteStates
    //StreamTodo: Implement GeneratedApi.RestartIce
    //StreamTodo: Rename GeneratedAPI to SfuRpcApi

    //StreamTodo: reconnect flow needs to send `UpdateSubscription` https://getstream.slack.com/archives/C022N8JNQGZ/p1691139853890859?thread_ts=1691139571.281779&cid=C022N8JNQGZ

    //StreamTodo: decide lifetime, if the obj persists across session maybe it should be named differently and only return struct handle to a session
    internal sealed class RtcSession : IMediaInputProvider, IDisposable
    {
        //StreamTodo: move to some config + perhaps allow user to set this
        public const ulong MaxPublishAudioBitrate = 500_000;
        public const ulong MaxPublishVideoBitrate = 1_200_000;

        public const ulong FullPublishVideoBitrate = 1_200_000;
        public const ulong HalfPublishVideoBitrate = MaxPublishVideoBitrate / 2;
        public const ulong QuarterPublishVideoBitrate = MaxPublishVideoBitrate / 4;

        // StreamTodo: control this via compiler flag
        public const bool LogWebRTCStats = false;

        // Some sources claim the 48kHz is the most optimal sample rate for WebRTC, other cause internal resampling
        public const int AudioInputSampleRate = 48_000;

        // Some sources claim the 48kHz is the most optimal sample rate for WebRTC, other cause internal resampling
        public const int AudioOutputSampleRate = 48_000;
        public const int AudioOutputChannels = 2;

#if STREAM_NATIVE_AUDIO
        public const bool UseNativeAudioBindings = true;
#else
        public const bool UseNativeAudioBindings = false;
#endif

        public event Action<bool> PublisherAudioTrackIsEnabledChanged;
        public event Action<bool> PublisherVideoTrackIsEnabledChanged;

        public event Action PublisherAudioTrackChanged;
        public event Action PublisherVideoTrackChanged;
        
        
        public bool PublisherAudioTrackIsEnabled
        {
            get => _publisherAudioTrackIsEnabled;
            private set
            {
                if (_publisherAudioTrackIsEnabled == value)
                {
                    return;
                }

                _publisherAudioTrackIsEnabled = value;
                InternalExecuteSetPublisherAudioTrackEnabled(value);
                
                PublisherAudioTrackIsEnabledChanged?.Invoke(value);
            }
        }

        public bool PublisherVideoTrackIsEnabled
        {
            get => _publisherVideoTrackIsEnabled;
            private set
            {
                if (_publisherVideoTrackIsEnabled == value)
                {
                    return;
                }

                _publisherVideoTrackIsEnabled = value;
                InternalExecuteSetPublisherVideoTrackEnabled(value);
                PublisherVideoTrackIsEnabledChanged?.Invoke(value);
            }
        }

        public CallingState CallState
        {
            get => _callState;
            private set
            {
                if (_callState == value)
                {
                    return;
                }

                var prevState = _callState;
                _callState = value;
                _logs.Info($"Call state changed from: `{prevState}` to: `{value}`");
            }
        }

        public StreamCall ActiveCall { get; private set; }

        public StreamPeerConnection Subscriber { get; private set; }
        public StreamPeerConnection Publisher { get; private set; }

        #region IInputProvider

        public event Action<AudioSource> AudioInputChanged;
        public event Action<WebCamTexture> VideoInputChanged;
        public event Action<Camera> VideoSceneInputChanged;

        //StreamTodo: move IInputProvider elsewhere. it's for easy testing only
        public AudioSource AudioInput
        {
            get => _audioInput;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (value == _audioInput)
                {
                    return;
                }

                var prev = _audioInput;
                _audioInput = value;

                if (prev != _audioInput)
                {
                    AudioInputChanged?.Invoke(value);
                }
            }
        }

        public WebCamTexture VideoInput
        {
            get => _videoInput;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (value == _videoInput)
                {
                    return;
                }

                var prev = _videoInput;
                _videoInput = value;

                if (prev != _videoInput)
                {
                    VideoInputChanged?.Invoke(value);
                }
            }
        }

        public Camera VideoSceneInput
        {
            get => _videoSceneInput;
            set
            {
                var prev = _videoSceneInput;
                _videoSceneInput = value;

                if (prev != _videoSceneInput)
                {
                    VideoSceneInputChanged?.Invoke(value);
                }
            }
        }

        #endregion

        public string SessionId { get; private set; }

        public RtcSession(SfuWebSocket sfuWebSocket, Func<IStreamCall, HttpClient> httpClientFactory, ILogs logs,
            ISerializer serializer, ITimeService timeService,
            IStreamClientConfig config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _timeService = timeService;
            _serializer = serializer;
            _logs = logs;

            //StreamTodo: SFU WS should be created here so that RTC session owns it
            _sfuWebSocket = sfuWebSocket ?? throw new ArgumentNullException(nameof(sfuWebSocket));

            var statsCollector = new UnityWebRtcStatsCollector(this, _serializer);
            _statsSender = new WebRtcStatsSender(this, statsCollector, _timeService, _logs);

            //StreamTodo: enable this only if a special mode e.g. compiler flag 
#if STREAM_AUDIO_BENCHMARK_ENABLED
            _logs.Warning($"Audio benchmark enabled. Waiting for a special video stream to measure audio-video sync. Check {nameof(VideoAudioSyncBenchmark)} summary for more details.");
            _videoAudioSyncBenchmark = new VideoAudioSyncBenchmark(_timeService, _logs);
#endif
        }

        public void Dispose()
        {
            StopAsync().LogIfFailed();

            _sfuWebSocket.Dispose();

            DisposeSubscriber();
            DisposePublisher();
        }

        //StreamTodo: to make updates more explicit we could make an UpdateService, that we could tell such dependency by constructor and component would self-register for updates
        public void Update()
        {
            _sfuWebSocket.Update();
            Publisher?.Update();
            _statsSender.Update();
            _videoAudioSyncBenchmark?.Update();

            //StreamTodo: we could remove this if we'd maintain a collection of tracks and update them directly
            if (ActiveCall != null && ActiveCall.Participants != null)
            {
                foreach (StreamVideoCallParticipant p in ActiveCall.Participants)
                {
                    p.Update();
                }
            }

            TryExecuteSubscribeToTracks();
        }

        public async Task SendWebRtcStats(SendStatsRequest request)
        {
            var response = await RpcCallAsync(request, GeneratedAPI.SendStats,
                nameof(GeneratedAPI.SendStats), postLog: LogWebRTCStats);

            if (ActiveCall == null)
            {
                //Ignore if call ended during this request
                return;
            }

            if (response.Error != null)
            {
                // StreamTodo: perhaps failure on stats sending should be silent? This can return "call not found" if call ended before `call.ended` event was received
                // Maybe we can wait 1-2s before displaying the error to cover this case

                _logs.Warning("Sending webRTC stats failed: " + response.Error.Message);
                _logs.ErrorIfDebug("Sending webRTC stats failed: " + response.Error.Message);
            }
        }

        //StreamTodo: solve this dependency better
        public void SetCache(ICache cache) => _cache = cache;

        public async Task StartAsync(StreamCall call)
        {
            if (ActiveCall != null)
            {
                throw new InvalidOperationException(
                    $"Cannot start new session until previous call is active. Active call: {ActiveCall}");
            }

            try
            {
                //StreamTodo: perhaps not necessary here
                ClearSession();

                SubscribeToSfuEvents();

                ActiveCall = call ?? throw new ArgumentNullException(nameof(call));
                _httpClient = _httpClientFactory(ActiveCall);

                CallState = CallingState.Joining;

                var sfuUrl = call.Credentials.Server.Url;
                var sfuToken = call.Credentials.Token;
                var iceServers = call.Credentials.IceServers;

                CreateSubscriber(iceServers);

                SessionId = Guid.NewGuid().ToString();

#if STREAM_DEBUG_ENABLED
                _logs.Info($"START Session: " + SessionId);
#endif

                // We don't set initial offer as local. Later on we set generated answer as a local
                var offer = await Subscriber.CreateOfferAsync();

                _sfuWebSocket.SetSessionData(SessionId, offer.sdp, sfuUrl, sfuToken);
                await _sfuWebSocket.ConnectAsync();

                while (CallState != CallingState.Joined)
                {
                    //StreamTodo: implement a timeout if something goes wrong
                    //StreamTodo: implement cancellation token
                    await Task.Delay(1);
                }

                // Wait for SFU connected to receive track prefix
                if (CanPublish())
                {
                    CreatePublisher(iceServers);
                }

                await SubscribeToTracksAsync();

                if (UseNativeAudioBindings)
                {
                    //StreamTODO: Either use UseNativeAudioBindings const or STREAM_NATIVE_AUDIO flag but not both. Once we replace the webRTC package we could remove STREAM_NATIVE_AUDIO
#if STREAM_NATIVE_AUDIO
                    WebRTC.StartAudioPlayback(AudioOutputSampleRate, AudioOutputChannels);
#endif
                }

                //StreamTodo: validate when this state should set
                CallState = CallingState.Joined;
                _videoAudioSyncBenchmark?.Init(call);
            }
            catch
            {
                ClearSession();
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (UseNativeAudioBindings)
            {
#if STREAM_NATIVE_AUDIO
                WebRTC.StopAudioPlayback();
#endif
            }

            if (ActiveCall != null)
            {
                _sfuWebSocket.SendLeaveCallRequest();
            }

            ClearSession();
            //StreamTodo: check with js definition of "offline" 
            CallState = CallingState.Offline;
            await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Video session stopped");
            _videoAudioSyncBenchmark?.Finish();
        }

        //StreamTodo: call by call.reconnectOrSwitchSfu()
        public void Reconnect()
        {
            Subscriber?.RestartIce();
            Publisher?.RestartIce();
        }

        public void UpdateRequestedVideoResolution(string participantSessionId, VideoResolution videoResolution)
        {
            _videoResolutionByParticipantSessionId[participantSessionId] = videoResolution;
            QueueTracksSubscriptionRequest();
        }

        public void SetAudioRecordingDevice(MicrophoneDeviceInfo device)
        {
            _logs.WarningIfDebug("RtcSession.SetAudioRecordingDevice device: " + device);
            _activeAudioRecordingDevice = device;
            UpdateAudioRecording();
        }

        /// <summary>
        /// Set Publisher Audio track enabled/disabled, if track is available, or store the preference for when track becomes available
        /// </summary>
        public void TrySetPublisherAudioTrackEnabled(bool isEnabled) => PublisherAudioTrackIsEnabled = isEnabled;

        public void TryRestartAudioRecording() => UpdateAudioRecording();

        public void TryRestartAudioPlayback()
        {
            if (!UseNativeAudioBindings)
            {
                return;
            }
#if STREAM_NATIVE_AUDIO
            WebRTC.StopAudioPlayback();
            WebRTC.StartAudioPlayback(AudioOutputSampleRate, AudioOutputChannels);
#endif
        }

        /// <summary>
        /// Set Publisher Video track enabled/disabled, if track is available, or store the preference for when track becomes available
        /// </summary>
        public void TrySetPublisherVideoTrackEnabled(bool isEnabled) => PublisherVideoTrackIsEnabled = isEnabled;

        private const float TrackSubscriptionDebounceTime = 0.1f;

        private readonly SfuWebSocket _sfuWebSocket;
        private readonly ISerializer _serializer;
        private readonly ILogs _logs;
        private readonly ITimeService _timeService;
        private readonly IStreamClientConfig _config;
        private readonly Func<IStreamCall, HttpClient> _httpClientFactory;
        private readonly WebRtcStatsSender _statsSender;
        private readonly VideoAudioSyncBenchmark _videoAudioSyncBenchmark;
        private readonly SdpMungeUtils _sdpMungeUtils = new SdpMungeUtils();

        private readonly List<SfuICETrickle> _pendingIceTrickleRequests = new List<SfuICETrickle>();
        private readonly PublisherVideoSettings _publisherVideoSettings = PublisherVideoSettings.Default;

        private readonly Dictionary<string, VideoResolution> _videoResolutionByParticipantSessionId
            = new Dictionary<string, VideoResolution>();

        private HttpClient _httpClient;
        private CallingState _callState;

        private ICache _cache;

        private float _lastTrackSubscriptionRequestTime;
        private bool _trackSubscriptionRequested;
        private bool _trackSubscriptionRequestInProgress;

        private bool _publisherAudioTrackIsEnabled;
        private bool _publisherVideoTrackIsEnabled;

        private AudioSource _audioInput;
        private WebCamTexture _videoInput;
        private Camera _videoSceneInput;
        
        private MicrophoneDeviceInfo _activeAudioRecordingDevice;

        private void ClearSession()
        {
            _pendingIceTrickleRequests.Clear();
            _videoResolutionByParticipantSessionId.Clear();

            Subscriber?.Dispose();
            Subscriber = null;
            Publisher?.Dispose();
            Publisher = null;

            ActiveCall = null;
            CallState = CallingState.Unknown;

            _trackSubscriptionRequested = false;
            _trackSubscriptionRequestInProgress = false;

            UnsubscribeFromSfuEvents();
        }

        //StreamTodo: request track subscriptions when SFU got changed. Android comment for setVideoSubscriptions:
        /*
         * - it sends the resolutions we're displaying the video at so the SFU can decide which track to send
         * - when switching SFU we should repeat this info
         * - http calls failing here breaks the call. (since you won't receive the video)
         * - we should retry continously until it works and after it continues to fail, raise an error that shuts down the call
         * - we retry when:
         * -- error isn't permanent, SFU didn't change, the mute/publish state didn't change
         * -- we cap at 30 retries to prevent endless loops
         */
        private void QueueTracksSubscriptionRequest()
        {
            if (_trackSubscriptionRequested)
            {
                return;
            }

            _trackSubscriptionRequested = true;
        }

        private void TryExecuteSubscribeToTracks()
        {
            if (!_trackSubscriptionRequested || _trackSubscriptionRequestInProgress)
            {
                return;
            }

            var timeSinceLastRequest = _timeService.Time - _lastTrackSubscriptionRequestTime;
            if (timeSinceLastRequest < TrackSubscriptionDebounceTime)
            {
                return;
            }

            SubscribeToTracksAsync().LogIfFailed();

            _lastTrackSubscriptionRequestTime = _timeService.Time;
            _trackSubscriptionRequested = false;
        }

        /// <summary>
        /// Request this via <see cref="QueueTracksSubscriptionRequest"/>. We don't want to call it too often
        /// </summary>
        private async Task SubscribeToTracksAsync()
        {
            if (_trackSubscriptionRequestInProgress)
            {
                QueueTracksSubscriptionRequest();
                return;
            }

            _trackSubscriptionRequestInProgress = true;

            var tracks = GetDesiredTracksDetails();

            var request = new UpdateSubscriptionsRequest
            {
                SessionId = SessionId,
            };
            request.Tracks.AddRange(tracks);

#if STREAM_DEBUG_ENABLED
            _logs.Info($"Request SFU - UpdateSubscriptionsRequest\n{_serializer.Serialize(request)}");
#endif

            var response = await RpcCallAsync(request, GeneratedAPI.UpdateSubscriptions,
                nameof(GeneratedAPI.UpdateSubscriptions));

            if (ActiveCall == null)
            {
                //Ignore if call ended during this request
                return;
            }

            if (response.Error != null)
            {
                _logs.Error(response.Error.Message);
            }

            _trackSubscriptionRequestInProgress = false;
        }

        private IEnumerable<TrackSubscriptionDetails> GetDesiredTracksDetails()
        {
            //StreamTodo: inject info on what tracks we want. Hardcoded audio & video but missing screenshare support
            var trackTypes = new[] { SfuTrackType.Video, SfuTrackType.Audio };

            foreach (var participant in ActiveCall.Participants)
            {
                if (participant.IsLocalParticipant)
                {
                    continue;
                }

                var requestedVideoResolution = GetRequestedVideoResolution(participant);

                foreach (var trackType in trackTypes)
                {
                    //StreamTODO: UserId is sometimes null here
                    //This was before changing the IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>.UpdateFromDto
                    //to extract UserId from User obj
                    
                    yield return new TrackSubscriptionDetails
                    {
                        UserId = GetUserId(participant),
                        SessionId = participant.SessionId,
                        TrackType = trackType,
                        Dimension = requestedVideoResolution.ToVideoDimension()
                    };
                }
            }
        }

        //StreamTodo: remove this, this is a workaround to Null UserId error
        private string GetUserId(IStreamVideoCallParticipant participant)
        {
            if (!string.IsNullOrEmpty(participant.UserId))
            {
                return participant.UserId;
            }

            if (participant.User == null || string.IsNullOrEmpty(participant.User.Id))
            {
                throw new Exception(
                    $"Both {nameof(IStreamVideoCallParticipant.UserId)} and {nameof(IStreamVideoCallParticipant.User)} ID are null or empty. No way to get the user ID");
            }

            return participant.User.Id;
        }

        private VideoResolution GetRequestedVideoResolution(IStreamVideoCallParticipant participant)
        {
            if (participant == null || participant.SessionId == null)
            {
#if STREAM_DEBUG_ENABLED
                _logs.Warning(
                    $"Participant or SessionId was null: {participant}, SeessionId: {participant?.SessionId}");
#endif
                return _config.Video.DefaultParticipantVideoResolution;
            }

            if (_videoResolutionByParticipantSessionId.TryGetValue(participant.SessionId, out var resolution))
            {
                return resolution;
            }

            return _config.Video.DefaultParticipantVideoResolution;
        }

        private async Task SendIceCandidateAsync(RTCIceCandidate candidate, StreamPeerType streamPeerType)
        {
            try
            {
                var iceTrickle = new SfuICETrickle
                {
                    PeerType = streamPeerType.ToPeerType(),
                    IceCandidate = _serializer.Serialize(candidate),
                    SessionId = SessionId,
                };

                if (_callState == CallingState.Joined)
                {
                    await RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle));
                }
                else
                {
                    _pendingIceTrickleRequests.Add(iceTrickle);
                }
            }
            catch (Exception e)
            {
                _logs.Exception(e);
            }
        }
        
        private void UpdateAudioRecording()
        {
            if (Publisher?.PublisherAudioTrack == null || !UseNativeAudioBindings)
            {
                return;
            }

#if STREAM_NATIVE_AUDIO
            var shouldRecord = _activeAudioRecordingDevice.IsValid && _publisherAudioTrackIsEnabled;

            if (shouldRecord)
            {
                //StreamTODO: implement proper passing deviceID -> for Android and IOS we're skipping the deviceID
                //because they operate on audio routing instead of actual devices. The underlying native implementation for Android let's OS pick the preferred device

                _logs.WarningIfDebug("RtcSession.TrySetAudioTrackEnabled -> Start local audio capture");
                Publisher.PublisherAudioTrack.StartLocalAudioCapture(-1, AudioInputSampleRate);
            }
            else
            {
                _logs.WarningIfDebug("RtcSession.TrySetAudioTrackEnabled -> Stop local audio capture");
                Publisher.PublisherAudioTrack.StopLocalAudioCapture();
            }
#endif
        }

        private void OnSfuJoinResponse(JoinResponse joinResponse)
        {
            _logs.InfoIfDebug($"Handle Sfu {nameof(JoinResponse)}");
            ActiveCall.UpdateFromSfu(joinResponse);
            OnSfuJoinedCall();
        }

        private void OnSfuJoinedCall()
        {
            CallState = CallingState.Joined;

            foreach (var iceTrickle in _pendingIceTrickleRequests)
            {
                RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle)).LogIfFailed();
            }
        }

        private void OnSfuIceTrickle(SfuICETrickle iceTrickle)
        {
            //StreamTodo: better to wrap in separate structure and not depend on a specific WebRTC implementation
            var iceCandidateInit = _serializer.Deserialize<RTCIceCandidateInit>(iceTrickle.IceCandidate);

            switch (iceTrickle.PeerType.ToStreamPeerType())
            {
                case StreamPeerType.Publisher:
                    Publisher.AddIceCandidate(iceCandidateInit);
                    break;
                case StreamPeerType.Subscriber:
                    Subscriber.AddIceCandidate(iceCandidateInit);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /**
     * This is called when the SFU sends us an offer
     * - Sets the remote description
     * - Creates an answer
     * - Sets the local description
     * - Sends the answer back to the SFU
     */
        private async void OnSfuSubscriberOffer(SubscriberOffer subscriberOffer)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning("OnSfuSubscriberOffer");
#endif
            //StreamTodo: check RtcSession.kt handleSubscriberOffer for the retry logic

            try
            {
                //StreamTodo: handle subscriberOffer.iceRestart
                var rtcSessionDescription = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = subscriberOffer.Sdp
                };

                await Subscriber.SetRemoteDescriptionAsync(rtcSessionDescription);
                Subscriber.ThrowDisposedDuringOperationIfNull();

                var answer = await Subscriber.CreateAnswerAsync();
                Subscriber.ThrowDisposedDuringOperationIfNull();

                //StreamTodo: mangle SDP

                await Subscriber.SetLocalDescriptionAsync(ref answer);
                Subscriber.ThrowDisposedDuringOperationIfNull();

                var sendAnswerRequest = new SendAnswerRequest
                {
                    PeerType = PeerType.Subscriber,
                    Sdp = answer.sdp,
                    SessionId = SessionId
                };

                await RpcCallAsync(sendAnswerRequest, GeneratedAPI.SendAnswer, nameof(GeneratedAPI.SendAnswer),
                    preLog: true);
            }
            catch (DisposedDuringOperationException)
            {
            }
            catch (Exception e)
            {
                _logs.Exception(e);
            }
        }

        private void OnSfuTrackUnpublished(TrackUnpublished trackUnpublished)
        {
            var userId = trackUnpublished.UserId;
            var sessionId = trackUnpublished.SessionId;
            var type = trackUnpublished.Type.ToPublicEnum();
            var cause = trackUnpublished.Cause;

            // Optionally available. Read TrackUnpublished.participant comment in events.proto
            var participantSfuDto = trackUnpublished.Participant;

            UpdateParticipantTracksState(userId, sessionId, type, isEnabled: false, out var participant);

            if (participantSfuDto != null && participant != null)
            {
                participant.UpdateFromSfu(participantSfuDto);
            }

            //StreamTodo: raise an event so user can react to track unpublished? Otherwise the video will just freeze
        }

        private void OnSfuTrackPublished(TrackPublished trackPublished)
        {
            var userId = trackPublished.UserId;
            var sessionId = trackPublished.SessionId;
            var type = trackPublished.Type.ToPublicEnum();

            // Optionally available. Read TrackUnpublished.participant comment in events.proto
            var participantSfuDto = trackPublished.Participant;

            UpdateParticipantTracksState(userId, sessionId, type, isEnabled: true, out var participant);

            if (participantSfuDto != null && participant != null)
            {
                participant.UpdateFromSfu(participantSfuDto);
            }

            //StreamTodo: fixes the case when joining a call where other participant starts with no video and activates video track after we've joined -
            // validated that this how Android/Js is handling this
            QueueTracksSubscriptionRequest();
        }

        private void UpdateParticipantTracksState(string userId, string sessionId, TrackType trackType, bool isEnabled,
            out StreamVideoCallParticipant participant)
        {
            participant = (StreamVideoCallParticipant)ActiveCall.Participants.FirstOrDefault(p
                => p.SessionId == sessionId);
            if (participant == null)
            {
                // This seems to be a valid case. When other participant joins we may receive TrackPublished event before we manage to subscribe for it
                return;
            }

            if (participant.IsLocalParticipant)
            {
                //StreamTODO: most probably expose RtcSession TrackStateChanged event so that AudioDeviceManager can subscribe

                switch (trackType)
                {
                    case TrackType.Unspecified:
                        break;
                    case TrackType.Audio:
                        TrySetPublisherAudioTrackEnabled(isEnabled);
                        break;
                    case TrackType.Video:
                        TrySetPublisherVideoTrackEnabled(isEnabled);
                        break;
                    case TrackType.ScreenShare:
                        break;
                    case TrackType.ScreenShareAudio:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null);
                }
                
                return;
            }

            participant.SetTrackEnabled(trackType, isEnabled);

            ActiveCall.NotifyTrackStateChanged(participant, trackType, isEnabled);
        }

        private async Task UpdateMuteStateAsync(TrackType trackType, bool isEnabled)
        {
            if (ActiveCall == null)
            {
                return;
            }
          
            await RpcCallAsync(new UpdateMuteStatesRequest
            {
                SessionId = SessionId,
                MuteStates =
                {
                    new TrackMuteState
                    {
                        TrackType = trackType.ToInternalEnum(),
                        Muted = !isEnabled
                    }
                }
            }, GeneratedAPI.UpdateMuteStates, nameof(GeneratedAPI.UpdateSubscriptions));
        }
        
        private void InternalExecuteSetPublisherAudioTrackEnabled(bool isEnabled)
        {
            if (Publisher?.PublisherAudioTrack == null)
            {
                _logs.WarningIfDebug("[Audio] RtcSession.InternalExecuteSetPublisherAudioTrackEnabled isEnabled: " + isEnabled + " -> track not available yet");
                return;
            }
            
            _logs.WarningIfDebug("[Audio] RtcSession.InternalExecuteSetPublisherAudioTrackEnabled isEnabled: " + isEnabled);

            Publisher.PublisherAudioTrack.Enabled = isEnabled;

            UpdateMuteStateAsync(TrackType.Audio, isEnabled).LogIfFailed();

            UpdateAudioRecording();
        }
        
        private void InternalExecuteSetPublisherVideoTrackEnabled(bool isEnabled)
        {
            if (Publisher?.PublisherVideoTrack == null)
            {
                return;
            }

            Publisher.PublisherVideoTrack.Enabled = isEnabled;
            
            UpdateMuteStateAsync(TrackType.Video, isEnabled).LogIfFailed();
        }

        private void OnSfuParticipantJoined(ParticipantJoined participantJoined)
        {
            if (!AssertCallIdMatch(ActiveCall, participantJoined.CallCid, _logs))
            {
                return;
            }

            ActiveCall.UpdateFromSfu(participantJoined, _cache);

            //StreamTodo: optimize with StringBuilder
            var id = $"{participantJoined.Participant.UserId}({participantJoined.Participant.SessionId})";
            _logs.Info($"Participant: {id} joined");

            QueueTracksSubscriptionRequest();
        }

        private void OnSfuParticipantLeft(ParticipantLeft participantLeft)
        {
            if (!AssertCallIdMatch(ActiveCall, participantLeft.CallCid, _logs))
            {
                return;
            }

            ActiveCall.UpdateFromSfu(participantLeft, _cache);

            //StreamTodo: optimize with StringBuilder
            var id = $"{participantLeft.Participant.UserId}({participantLeft.Participant.SessionId})";
            _logs.Info($"Participant: {id} left");

            _videoResolutionByParticipantSessionId.Remove(participantLeft.Participant.SessionId);

            QueueTracksSubscriptionRequest();
        }

        private void OnSfuDominantSpeakerChanged(DominantSpeakerChanged dominantSpeakerChanged)
        {
            ActiveCall.UpdateFromSfu(dominantSpeakerChanged, _cache);
        }

        private void OnSfuWebSocketOnError(SfuError obj)
        {
            _logs.Error(
                $"Sfu Error - Code: {obj.Error_.Code}, Message: {obj.Error_.Message}, ShouldRetry: {obj.Error_.ShouldRetry}");
        }

        private void OnSfuPinsUpdated(PinsChanged pinsChanged)
        {
            ActiveCall.UpdateFromSfu(pinsChanged, _cache);
        }

        private void OnSfuIceRestart(ICERestart iceRestart)
        {
            // StreamTODO: Implement OnSfuIceRestart
        }

        private void OnSfuGoAway(GoAway goAway)
        {
            // StreamTODO: Implement OnSfuGoAway
        }

        private void OnSfuCallGrantsUpdated(CallGrantsUpdated callGrantsUpdated)
        {
            // StreamTODO: Implement OnSfuCallGrantsUpdated
        }

        private void OnSfuChangePublishQuality(ChangePublishQuality changePublishQuality)
        {
            // StreamTODO: Implement OnSfuChangePublishQuality
        }

        private void OnSfuConnectionQualityChanged(ConnectionQualityChanged connectionQualityChanged)
        {
            // StreamTODO: Implement OnSfuConnectionQualityChanged
        }

        private void OnSfuAudioLevelChanged(AudioLevelChanged audioLevelChanged)
        {
            // StreamTODO: Implement OnSfuAudioLevelChanged
        }

        private void OnSfuPublisherAnswer(PublisherAnswer publisherAnswer)
        {
            // StreamTODO: Implement OnSfuPublisherAnswer
        }

        private void OnSfuWebSocketOnChangePublishOptions(ChangePublishOptions obj)
        {
            // StreamTODO: Implement OnSfuWebSocketOnChangePublishOptions
        }

        private void OnSfuWebSocketOnParticipantMigrationComplete()
        {
            // StreamTODO: Implement OnSfuWebSocketOnParticipantMigrationComplete
        }

        private void OnSfuWebSocketOnParticipantUpdated(ParticipantUpdated obj)
        {
            // StreamTODO: Implement OnSfuWebSocketOnParticipantUpdated
        }

        private void OnSfuWebSocketOnCallEnded()
        {
            // StreamTODO: Implement OnSfuWebSocketOnCallEnded
        }

        //StreamTodo: implement retry strategy like in Android SDK
        //If possible, take into account if we the update is still valid e.g. 
        private async Task<TResponse> RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, Task<TResponse>> rpcCallAsync, string debugRequestName, bool preLog = false,
            bool postLog = true)
        {
            //StreamTodo: use rpcCallAsync.GetMethodInfo().Name; instead debugRequestName

#if STREAM_DEBUG_ENABLED
            var serializedRequest = _serializer.Serialize(request);
            if (preLog)
            {
                _logs.Warning($"[RPC REQUEST START] {debugRequestName} {serializedRequest}");
            }
#endif

            var response = await rpcCallAsync(_httpClient, request);

#if STREAM_DEBUG_ENABLED
            if (postLog)
            {
                var serializedResponse = _serializer.Serialize(response);

                //StreamTodo: move to debug helper class
                var sb = new System.Text.StringBuilder();

                var errorProperty = typeof(TResponse).GetProperty("Error");
                var error = (StreamVideo.v1.Sfu.Models.Error)errorProperty.GetValue(response);
                var errorLog = error != null ? $"<color=red>{error.Message}</color>" : "";
                var errorStatus = error != null ? "<color=red>FAILED</color>" : "<color=green>SUCCESS</color>";
                sb.AppendLine($"[RPC Request] {errorStatus} {debugRequestName} | {errorLog}");
                sb.AppendLine(serializedRequest);
                sb.AppendLine();
                sb.AppendLine("Response:");
                sb.AppendLine(serializedResponse);

                _logs.Warning(sb.ToString());
            }
#endif

            return response;
        }

        //StreamTodo: subscribe to changes in capabilities. This can potentially change during the call
        private bool CanPublish()
            => ActiveCall != null &&
               ActiveCall.OwnCapabilities.Any(c => c == OwnCapability.SendVideo || c == OwnCapability.SendAudio);

        /**
     * https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/negotiationneeded_event
     *
     * Is called whenever a negotiation is needed. Common examples include
     * - Adding a new media stream
     * - Adding an audio Stream
     * - A screenshare track is started
     *
     * Creates a new SDP
     * - And sets it on the localDescription
     * - Enables video simulcast
     * - calls setPublisher
     * - sets setRemoteDescription
     *
     * Retry behaviour is to retry 3 times quickly as long as
     * - the sfu didn't change
     * - the sdp didn't change
     * If that fails ask the call monitor to do an ice restart
     */
        private async void OnPublisherNegotiationNeeded()
        {
#if STREAM_DEBUG_ENABLED
            Debug.LogWarning("OnPublisherNegotiationNeeded");
#endif

            try
            {
                if (Publisher.SignalingState != RTCSignalingState.Stable)
                {
                    _logs.Error(
                        $"{nameof(Publisher.SignalingState)} state is not stable, current state: {Publisher.SignalingState}");
                }

                var offer = await Publisher.CreateOfferAsync();
                Publisher.ThrowDisposedDuringOperationIfNull();

                //StreamTOodo: check if SDP is null or empty (this would throw an exception during setting)

                //StreamTodo: ignored the _config.Audio.EnableRed because with current webRTC version this modification causes a crash
                //We're also forcing the red codec in the StreamPeerConnection but atm this results in "InvalidModification"
                //This is most likely issue with the webRTC lib
                if (_config.Audio.EnableDtx)
                {
                    offer = new RTCSessionDescription()
                    {
                        type = offer.type,
                        sdp = _sdpMungeUtils.ModifySdp(offer.sdp, enableRed: false, _config.Audio.EnableDtx)
                    };

                    _logs.Info(
                        $"Modified SDP, enable red: {_config.Audio.EnableRed}, enable DTX: {_config.Audio.EnableDtx} ");
                }

                await Publisher.SetLocalDescriptionAsync(ref offer);
                Publisher.ThrowDisposedDuringOperationIfNull();

                // //StreamTodo: timeout + break if we're disconnecting/reconnecting
                // while (_sfuWebSocket.ConnectionState != ConnectionState.Connected)
                // {
                //     await Task.Delay(1);
                // }

#if STREAM_DEBUG_ENABLED
                _logs.Warning($"[Publisher] LocalDesc (SDP Offer):\n{offer.sdp}");
#endif

                var tracks = GetPublisherTracks(offer.sdp);

                //StreamTodo: mangle SDP
                var request = new SetPublisherRequest
                {
                    Sdp = offer.sdp,
                    SessionId = SessionId,
                };
                request.Tracks.AddRange(tracks);

#if STREAM_DEBUG_ENABLED
                var serializedRequest = _serializer.Serialize(request);
                _logs.Warning($"SetPublisherRequest:\n{serializedRequest}");
#endif

                var result = await RpcCallAsync(request, GeneratedAPI.SetPublisher, nameof(GeneratedAPI.SetPublisher));
                Publisher.ThrowDisposedDuringOperationIfNull();

#if STREAM_DEBUG_ENABLED
                _logs.Warning($"[Publisher] RemoteDesc (SDP Answer):\n{result.Sdp}");
#endif

                await Publisher.SetRemoteDescriptionAsync(new RTCSessionDescription()
                {
                    type = RTCSdpType.Answer,
                    sdp = result.Sdp
                });
            }
            catch (DisposedDuringOperationException)
            {
            }
            catch (Exception e)
            {
                _logs.Exception(e);
            }
        }

        private string ExtractVideoTrackId(string sdp)
        {
            var lines = sdp.Split("\n");
            var mediaStreamRecord
                = lines.Single(l => l.StartsWith($"a=msid:{Publisher.PublisherVideoMediaStream.Id}"));
            var parts = mediaStreamRecord.Split(" ");
            var result = parts[1];

            // StreamTodo: verify if this is needed
            result = result.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

            return result;
        }

        private IEnumerable<TrackInfo> GetPublisherTracks(string sdp)
        {
            var transceivers = Publisher.GetTransceivers().ToArray();

            //StreamTodo: investigate why this return no results
            // var senderTracks = _publisher.GetTransceivers().Where(t
            //     => t.Direction == RTCRtpTransceiverDirection.SendOnly && t.Sender?.Track != null).ToArray();

#if STREAM_DEBUG_ENABLED
            _logs.Warning($"GetPublisherTracks - transceivers: {transceivers?.Count()} ");
#endif

            //StreamTodo: figure out TrackType, because we rely on transceiver track type mapping we don't support atm screen video/audio share tracks
            //This implementation is based on the Android SDK, perhaps we shouldn't rely on GetTransceivers() but maintain our own TrackType => Transceiver mapping

            foreach (var t in transceivers)
            {
                var trackId = t.Sender.Track.Kind == TrackKind.Video ? ExtractVideoTrackId(sdp) : t.Sender.Track.Id;

                if (t.Mid == null)
                {
                    // StreamTodo: figure out why this is happening and if there should be any re-try logic. This is part of the SDP negotiation
                    // Perhaps we need to recreate transceiver. 
                    // This is happening only sometimes 
#if STREAM_DEBUG_ENABLED
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Error: Track MID was NULL. Transceiver dump:");
                    sb.AppendLine(DebugObjectPrinter.PrintObject(t));
                    sb.AppendLine("SDP:");
                    sb.AppendLine(sdp);
                    _logs.Error(sb.ToString());
#endif

                    t.Stop();

                    continue;
                }

                var trackInfo = new TrackInfo
                {
                    TrackId = trackId,
                    TrackType = t.Sender.Track.Kind.ToInternalEnum(),
                    Mid = t.Mid // Will throw if NULL due to protbuf precondition 
                };

                if (t.Sender.Track.Kind == TrackKind.Video)
                {
                    var videoLayers = GetPublisherVideoLayers(Publisher.VideoSender.GetParameters().encodings,
                        _publisherVideoSettings);
                    trackInfo.Layers.AddRange(videoLayers);

#if STREAM_DEBUG_ENABLED
                    _logs.Warning(
                        $"Video layers: {videoLayers.Count()} for transceiver: {t.Sender.Track.Kind}, Sender Track ID: {t.Sender.Track.Id}");
#endif
                }

                yield return trackInfo;
            }
        }

        private string ReplaceVp8PayloadType(string sdpOffer)
        {
            string[] patterns =
            {
                @"m=video 9 UDP/TLS/RTP/SAVPF 127",
                @"a=rtpmap:127 VP8/90000",
                @"a=rtcp-fb:127 goog-remb",
                @"a=rtcp-fb:127 transport-cc",
                @"a=rtcp-fb:127 ccm fir",
                @"a=rtcp-fb:127 nack",
                @"a=rtcp-fb:127 nack pli",
                @"a=fmtp:127"
            };

            foreach (var pattern in patterns)
            {
                sdpOffer = Regex.Replace(sdpOffer, pattern, pattern.Replace("127", "96"));
            }

            return sdpOffer;
        }

        private IEnumerable<VideoLayer> GetPublisherVideoLayers(IEnumerable<RTCRtpEncodingParameters> encodings,
            PublisherVideoSettings videoSettings)
        {
#if STREAM_DEBUG_ENABLED
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("GetPublisherVideoLayers:");
#endif
            foreach (var encoding in encodings)
            {
                var scaleBy = encoding.scaleResolutionDownBy ?? 1.0;
                var resolution = videoSettings.MaxResolution;
                var width = (uint)(resolution.Width / scaleBy);
                var height = (uint)(resolution.Height / scaleBy);

                var quality = EncodingsToVideoQuality(encoding);

#if STREAM_DEBUG_ENABLED
                sb.AppendLine(
                    $"- rid: {encoding.rid} quality: {quality}, scaleBy: {scaleBy}, width: {width}, height: {height}, bitrate: {encoding.maxBitrate}");
#endif

                yield return new VideoLayer
                {
                    Rid = string.IsNullOrEmpty(encoding.rid) ? "f" : encoding.rid,
                    VideoDimension = new VideoDimension
                    {
                        Width = width,
                        Height = height
                    },
                    Bitrate = (uint)(encoding.maxBitrate ?? 0),
                    Fps = encoding.maxFramerate.GetValueOrDefault(30),
                    Quality = quality,
                };
            }

#if STREAM_DEBUG_ENABLED
            _logs.Warning(sb.ToString());
#endif
        }

        private static VideoQuality EncodingsToVideoQuality(RTCRtpEncodingParameters encodings)
        {
            //StreamTodo: probably remove this or put as DEBUG_ONLY, this is only needed when testing with single video layer because `rid` is set only when simulcasting
            if (string.IsNullOrEmpty(encodings.rid))
            {
                switch (encodings.maxBitrate)
                {
                    case FullPublishVideoBitrate: return VideoQuality.High;
                    case HalfPublishVideoBitrate: return VideoQuality.Mid;
                    default: return VideoQuality.LowUnspecified;
                }
            }

            switch (encodings.rid)
            {
                case "f": return VideoQuality.High;
                case "h": return VideoQuality.Mid;
                default: return VideoQuality.LowUnspecified;
            }
        }

        private void OnIceTrickled(RTCIceCandidate iceCandidate, StreamPeerType peerType)
        {
            SendIceCandidateAsync(iceCandidate, peerType).LogIfFailed();
        }

        private void OnSubscriberStreamAdded(MediaStream mediaStream)
        {
            var idParts = mediaStream.Id.Split(":");
            var trackPrefix = idParts[0];
            var trackTypeKey = idParts[1];

#if STREAM_DEBUG_ENABLED
            _logs.Warning($"Subscriber stream received, trackPrefix: {trackPrefix}, trackTypeKey: {trackTypeKey}");
#endif

            var participant = ActiveCall.Participants.SingleOrDefault(p => p.TrackLookupPrefix == trackPrefix);
            if (participant == null)
            {
                //StreamTodo: figure out severity of this case. Perhaps it's not an error, maybe we haven't received coordinator event yet like ParticipantJoined
                _logs.Warning(
                    $"Failed to find participant with trackPrefix: {trackPrefix} for media stream with ID: {mediaStream.Id}");
                return;
            }

            if (!TrackTypeExt.TryGetTrackType(trackTypeKey, out var trackType))
            {
                _logs.Error(
                    $"Failed to get {typeof(TrackType)} for value: {trackTypeKey} on media stream with ID: {mediaStream.Id}");
                return;
            }

            if (trackType == TrackType.Unspecified)
            {
                _logs.Error(
                    $"Unexpected {nameof(trackType)} of value: {trackType} on media stream with ID: {mediaStream.Id}");
                return;
            }

            //StreamTodo: assert that we expect exactly one track per type.
            //In theory stream can contain multiple tracks but we're extracting track type from stream ID so I assume it always has to be exactly one track

            foreach (var track in mediaStream.GetAudioTracks())
            {
                //StreamTodo: verify why this is needed. Taken from Android SDK
                track.Enabled = true;
            }

            var internalParticipant = ((StreamVideoCallParticipant)participant);

            foreach (var track in mediaStream.GetTracks())
            {
                internalParticipant.SetTrack(trackType, track, out var streamTrack);
                ActiveCall.NotifyTrackAdded(internalParticipant, streamTrack);
            }
        }

        private void CreateSubscriber(IEnumerable<ICEServer> iceServers)
        {
            Subscriber = new StreamPeerConnection(_logs, StreamPeerType.Subscriber, iceServers,
                this, _config.Audio, _publisherVideoSettings);
            Subscriber.IceTrickled += OnIceTrickled;
            Subscriber.StreamAdded += OnSubscriberStreamAdded;
        }

        private void DisposeSubscriber()
        {
            if (Subscriber != null)
            {
                Subscriber.IceTrickled -= OnIceTrickled;
                Subscriber.StreamAdded -= OnSubscriberStreamAdded;
                Subscriber.Dispose();
                Subscriber = null;
            }
        }

        /// <summary>
        /// Creating publisher requires active <see cref="IStreamCall"/>
        /// </summary>
        private void CreatePublisher(IEnumerable<ICEServer> iceServers)
        {
            //StreamTodo: Handle default settings -> speaker off, mic off, cam off
            var callSettings = ActiveCall.Settings;

            Publisher = new StreamPeerConnection(_logs, StreamPeerType.Publisher, iceServers,
                this, _config.Audio, _publisherVideoSettings);
            Publisher.IceTrickled += OnIceTrickled;
            Publisher.NegotiationNeeded += OnPublisherNegotiationNeeded;
            Publisher.PublisherAudioTrackChanged += OnPublisherAudioTrackChanged;
            Publisher.PublisherVideoTrackChanged += OnPublisherVideoTrackChanged;

            Publisher.InitPublisherTracks();

            TrySetPublisherAudioTrackEnabled(_publisherAudioTrackIsEnabled);
            TrySetPublisherVideoTrackEnabled(_publisherVideoTrackIsEnabled);
        }

        private void DisposePublisher()
        {
            if (Publisher != null)
            {
                Publisher.IceTrickled -= OnIceTrickled;
                Publisher.NegotiationNeeded -= OnPublisherNegotiationNeeded;
                Publisher.PublisherAudioTrackChanged -= OnPublisherAudioTrackChanged;
                Publisher.PublisherVideoTrackChanged -= OnPublisherVideoTrackChanged;
                Publisher.Dispose();
                Publisher = null;
            }
        }

        private void OnPublisherAudioTrackChanged(AudioStreamTrack audioTrack)
        {
            UpdateAudioRecording();
            PublisherAudioTrackChanged?.Invoke();
        }
        
        void OnPublisherVideoTrackChanged(VideoStreamTrack videoTrack)
        {
            PublisherVideoTrackChanged?.Invoke();
        }

        private static bool AssertCallIdMatch(IStreamCall activeCall, string callId, ILogs logs)
        {
            if (callId != null && activeCall?.Cid != callId)
            {
                var activeCallIdLog = activeCall == null
                    ? $"{nameof(activeCall)} is null"
                    : $"{nameof(activeCall)} is {activeCall.Id}";
                logs.Error($"Received {nameof(ParticipantJoined)} event for call ID: {callId} but {activeCallIdLog}");
                return false;
            }

            return true;
        }

        private void SubscribeToSfuEvents()
        {
            _sfuWebSocket.SubscriberOffer += OnSfuSubscriberOffer;
            _sfuWebSocket.PublisherAnswer += OnSfuPublisherAnswer;
            _sfuWebSocket.ConnectionQualityChanged += OnSfuConnectionQualityChanged;
            _sfuWebSocket.AudioLevelChanged += OnSfuAudioLevelChanged;
            _sfuWebSocket.IceTrickle += OnSfuIceTrickle;
            _sfuWebSocket.ChangePublishQuality += OnSfuChangePublishQuality;
            _sfuWebSocket.ParticipantJoined += OnSfuParticipantJoined;
            _sfuWebSocket.ParticipantLeft += OnSfuParticipantLeft;
            _sfuWebSocket.DominantSpeakerChanged += OnSfuDominantSpeakerChanged;
            _sfuWebSocket.JoinResponse += OnSfuJoinResponse;
            _sfuWebSocket.TrackPublished += OnSfuTrackPublished;
            _sfuWebSocket.TrackUnpublished += OnSfuTrackUnpublished;
            _sfuWebSocket.Error += OnSfuWebSocketOnError;
            _sfuWebSocket.CallGrantsUpdated += OnSfuCallGrantsUpdated;
            _sfuWebSocket.GoAway += OnSfuGoAway;
            _sfuWebSocket.IceRestart += OnSfuIceRestart;
            _sfuWebSocket.CallEnded += OnSfuWebSocketOnCallEnded;
            _sfuWebSocket.ParticipantUpdated += OnSfuWebSocketOnParticipantUpdated;
            _sfuWebSocket.ParticipantMigrationComplete += OnSfuWebSocketOnParticipantMigrationComplete;
            _sfuWebSocket.ChangePublishOptions += OnSfuWebSocketOnChangePublishOptions;
        }

        private void UnsubscribeFromSfuEvents()
        {
            _sfuWebSocket.SubscriberOffer -= OnSfuSubscriberOffer;
            _sfuWebSocket.PublisherAnswer -= OnSfuPublisherAnswer;
            _sfuWebSocket.ConnectionQualityChanged -= OnSfuConnectionQualityChanged;
            _sfuWebSocket.AudioLevelChanged -= OnSfuAudioLevelChanged;
            _sfuWebSocket.IceTrickle -= OnSfuIceTrickle;
            _sfuWebSocket.ChangePublishQuality -= OnSfuChangePublishQuality;
            _sfuWebSocket.ParticipantJoined -= OnSfuParticipantJoined;
            _sfuWebSocket.ParticipantLeft -= OnSfuParticipantLeft;
            _sfuWebSocket.DominantSpeakerChanged -= OnSfuDominantSpeakerChanged;
            _sfuWebSocket.JoinResponse -= OnSfuJoinResponse;
            _sfuWebSocket.TrackPublished -= OnSfuTrackPublished;
            _sfuWebSocket.TrackUnpublished -= OnSfuTrackUnpublished;
            _sfuWebSocket.Error -= OnSfuWebSocketOnError;
            _sfuWebSocket.CallGrantsUpdated -= OnSfuCallGrantsUpdated;
            _sfuWebSocket.GoAway -= OnSfuGoAway;
            _sfuWebSocket.IceRestart -= OnSfuIceRestart;
            _sfuWebSocket.PinsUpdated -= OnSfuPinsUpdated;
            _sfuWebSocket.CallEnded -= OnSfuWebSocketOnCallEnded;
            _sfuWebSocket.ParticipantUpdated -= OnSfuWebSocketOnParticipantUpdated;
            _sfuWebSocket.ParticipantMigrationComplete -= OnSfuWebSocketOnParticipantMigrationComplete;
            _sfuWebSocket.ChangePublishOptions -= OnSfuWebSocketOnChangePublishOptions;
        }
    }
}