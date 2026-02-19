#if UNITY_ANDROID && !UNITY_EDITOR
#define STREAM_NATIVE_AUDIO //Defined in multiple files
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.v1.Sfu.Events;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using StreamVideo.Core.Configs;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.Exceptions;
using StreamVideo.Core.InternalDTO.Requests;
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
using StreamVideo.Core.Trace;
using StreamVideo.Libs.NetworkMonitors;

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
    internal sealed class RtcSession : IMediaInputProvider, ISfuClient, IDisposable
    {
        // Static session counter to track the number of sessions created
        private static int _sessionCounter = 0;

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

        public const int MaxParticipantsForVideoAutoSubscription = 5;

        public event Action<bool> PublisherAudioTrackIsEnabledChanged;
        public event Action<bool> PublisherVideoTrackIsEnabledChanged;

        public event Action PublisherAudioTrackChanged;
        public event Action PublisherVideoTrackChanged;

        public event Action PeerConnectionDisconnectedDuringSession;

        /// <summary>
        /// Fired when the SFU WebSocket disconnects unexpectedly.
        /// This is NOT fired when the disconnect is intentional (e.g., leaving a call).
        /// </summary>
        public event Action SfuDisconnected;

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

        public SubscriberPeerConnection Subscriber { get; private set; }
        public PublisherPeerConnection Publisher { get; private set; }

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

        public SessionID SessionId { get; } = new SessionID();
        
        /// <summary>
        /// Current session version. Increments when session ID is regenerated (e.g., during rejoin).
        /// Used to invalidate stale operations like pending ICE restarts.
        /// </summary>
        public int SessionVersion => SessionId.Version;

        public RtcSession(ISfuWebSocketFactory sfuWebSocketFactory, Func<IStreamCall, HttpClient> httpClientFactory,
            ILogs logs, ISerializer serializer, ITimeService timeService, StreamVideoLowLevelClient lowLevelClient,
            IStreamClientConfig config, INetworkMonitor networkMonitor)
        {
            _httpClientFactory = httpClientFactory;
            _logs = logs;
            _serializer = serializer;
            _timeService = timeService;
            _lowLevelClient = lowLevelClient;
            _config = config;
            _networkMonitor = networkMonitor;
            _sfuWebSocketFactory = sfuWebSocketFactory ?? throw new ArgumentNullException(nameof(sfuWebSocketFactory));

            var statsCollector = new UnityWebRtcStatsCollector(this, _serializer, _tracerManager);
            _statsSender = new WebRtcStatsSender(this, statsCollector, _timeService, _logs);

            //StreamTodo: enable this only if a special mode e.g. compiler flag 
#if STREAM_AUDIO_BENCHMARK_ENABLED
            _logs.Warning($"Audio benchmark enabled. Waiting for a special video stream to measure audio-video sync. Check {nameof(VideoAudioSyncBenchmark)} summary for more details.");
            _videoAudioSyncBenchmark = new VideoAudioSyncBenchmark(_timeService, _logs);
#endif

            _networkMonitor.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Dispose()
        {
            StopAsync("Video Client is being disposed").LogIfFailed();

            DisposeSfuWebSocket();

            DisposeSubscriber();
            DisposePublisher();

            _tracerManager?.Clear();

            _networkMonitor.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }

        //StreamTodo: to make updates more explicit we could make an UpdateService, that we could tell such dependency by constructor and component would self-register for updates
        public void Update()
        {
            _networkMonitor.Update();
            _sfuWebSocket?.Update();
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

            TryExecutePendingReconnectRequest();
        }

        public async Task SendWebRtcStats(SendStatsRequest request, CancellationToken cancellationToken)
        {
            var response = await RpcCallAsync(request, GeneratedAPI.SendStats,
                nameof(GeneratedAPI.SendStats), cancellationToken, response => response.Error,
                postLog: LogWebRTCStats);

            if (ActiveCall == null)
            {
                //Ignore if call ended during this request
#if STREAM_DEBUG_ENABLED
                _logs.Warning("Sending webRTC stats aborted: call ended during the request");
#endif

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

        private void ValidateCallCredentialsOrThrow(IStreamCall call)
        {
            if (call.Credentials == null)
            {
                throw new ArgumentNullException(nameof(call.Credentials));
            }

            if (call.Credentials.Server == null)
            {
                throw new ArgumentNullException(nameof(call.Credentials.Server));
            }

            if (string.IsNullOrEmpty(call.Credentials.Server.Url))
            {
                throw new ArgumentNullException(nameof(call.Credentials.Server.Url));
            }

            if (string.IsNullOrEmpty(call.Credentials.Token))
            {
                throw new ArgumentNullException(nameof(call.Credentials.Token));
            }

            if (call.Credentials.IceServers == null)
            {
                throw new ArgumentNullException(nameof(call.Credentials.IceServers));
            }

            if (call.Credentials.IceServers.Count == 0)
            {
                throw new ArgumentException("At least one ICE server must be provided in call credentials.");
            }
        }

        //public void SetCallingState(CallingState newState) => CallState = newState;

//         public async Task StartAsync(StreamCall call, CancellationToken cancellationToken = default)
//         {
//             if (ActiveCall != null)
//             {
//                 throw new InvalidOperationException(
//                     $"Cannot start new session until previous call is active. Active call: {ActiveCall}");
//             }
//
//             if (call == null)
//             {
//                 throw new ArgumentNullException(nameof(call));
//             }
//
//             try
//             {
//                 _logs.Info($"Start joining a call: type={call.Type}, id={call.Id}");
//
//                 //StreamTodo: perhaps not necessary here
//                 ClearSession();
//
//                 if (_joinCallCts != null)
//                 {
//                     _logs.ErrorIfDebug("Previous join call CTS was not cleaned up properly. Cancelling it now.");
//                     _joinCallCts.Cancel();
//                     _joinCallCts.Dispose();
//                     _joinCallCts = null;
//                 }
//
//                 if (_activeCallCts != null)
//                 {
//                     _logs.ErrorIfDebug("Previous active call CTS was not cleaned up properly. Cancelling it now.");
//                     _activeCallCts.Cancel();
//                     _activeCallCts.Dispose();
//                     _activeCallCts = null;
//                 }
//
//                 _joinCallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//
//                 SubscribeToSfuEvents();
//
//                 ActiveCall = call;
//                 _httpClient = _httpClientFactory(ActiveCall);
//
//                 CallState = CallingState.Joining;
//
//                 ValidateCallCredentialsOrThrow(ActiveCall);
//
//                 var sfuUrl = call.Credentials.Server.Url;
//                 var sfuToken = call.Credentials.Token;
//                 var iceServers = call.Credentials.IceServers;
//
//                 // Initialize tracers with the correct ID format - separate tracers for SFU, Publisher, and Subscriber
//                 var sfuUrlForId = sfuUrl.Replace("https://", "").Replace("/twirp", "");
//                 var sessionNumber = _sessionCounter + 1;
//                 _sfuTracer = _tracerManager.GetTracer($"{sessionNumber}-{sfuUrlForId}");
//                 _publisherTracer = _tracerManager.GetTracer($"{sessionNumber}-pub");
//                 _subscriberTracer = _tracerManager.GetTracer($"{sessionNumber}-sub");
//                 _sessionCounter++;
//
//                 CreateSubscriber(iceServers);
//
//                 SessionId.Regenerate();
//
// #if STREAM_DEBUG_ENABLED
//                 _logs.Info($"START Session: " + SessionId);
// #endif
//
//                 // We don't set initial offer as local. Later on we set generated answer as a local
//                 var subscriberOffer = await Subscriber.CreateOfferAsync(_joinCallCts.Token);
//                 var publisherOffer = await Publisher.CreateOfferAsync(_joinCallCts.Token);
//
//                 if (string.IsNullOrEmpty(subscriberOffer.sdp))
//                 {
//                     throw new ArgumentException("Generated offer SDP is null or empty");
//                 }
//
//                 _sfuWebSocket.InitNewSession(SessionId, sfuUrl, sfuToken, subscriberOffer.sdp, publisherOffer.sdp);
//                 await _sfuWebSocket.ConnectAsync(default, cancellationToken);
//
// #if STREAM_TESTS_ENABLED && UNITY_EDITOR
//                 // Simulate a bit of delay for tests so we can test killing the operation in progress
//                 //StreamTOdo: we could add fake delays in multiple places and this way control exiting from every step in tests
//                 await Task.Delay(100);
// #endif
//
//                 // Wait for call to be joined with timeout
//                 const int joinTimeoutSeconds = 30;
//                 var joinStartTime = _timeService.Time;
//                 while (CallState != CallingState.Joined)
//                 {
//                     await Task.Delay(1, cancellationToken);
//                     cancellationToken.ThrowIfCancellationRequested();
//
//                     var elapsedTime = _timeService.Time - joinStartTime;
//                     if (elapsedTime > joinTimeoutSeconds)
//                     {
//                         throw new TimeoutException(
//                             $"Failed to join call within {joinTimeoutSeconds} seconds. Current state: {CallState}");
//                     }
//                 }
//
//                 // Wait for SFU connected to receive track prefix
//                 if (CanPublish())
//                 {
//                     CreatePublisher(iceServers);
//                 }
//
//                 await SubscribeToTracksAsync(cancellationToken);
//
//                 if (UseNativeAudioBindings)
//                 {
//                     //StreamTODO: Either use UseNativeAudioBindings const or STREAM_NATIVE_AUDIO flag but not both. Once we replace the webRTC package we could remove STREAM_NATIVE_AUDIO
// #if STREAM_NATIVE_AUDIO
//                     WebRTC.StartAudioPlayback(AudioOutputSampleRate, AudioOutputChannels);
// #endif
//                 }
//
//                 foreach (var p in ActiveCall.Participants)
//                 {
//                     NotifyParticipantJoined(p.SessionId);
//                 }
//
//                 //StreamTodo: validate when this state should set
//                 CallState = CallingState.Joined;
//
//                 _activeCallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//
//                 _logs.Info($"Joined call: type={call.Type}, id={call.Id}");
//
// #if STREAM_DEBUG_ENABLED
//                 _videoAudioSyncBenchmark?.Init(call);
// #endif
//             }
//             catch (OperationCanceledException)
//             {
//                 ClearSession();
//                 throw;
//             }
//             catch (Exception e)
//             {
//                 _logs.Exception(e);
//                 ClearSession();
//                 throw;
//             }
//             finally
//             {
//                 if (_joinCallCts != null)
//                 {
//                     _joinCallCts.Dispose();
//                     _joinCallCts = null;
//                 }
//             }
//         }

        public async Task Join(JoinCallData joinCallData, CancellationToken cancellationToken)
        {
            if (CallState == CallingState.Joined)
            {
                throw new InvalidOperationException(
                    $"Call is already joined. Please leave the current call before joining a new one.");
            }

            if (CallState == CallingState.Joining)
            {
                throw new InvalidOperationException(
                    $"Joining a `{_joinCallData.Id}` call is in progress. Please cancel the current join operation before joining a new one.");
            }

            // we will count the number of join failures per SFU.
            // once the number of failures reaches 2, we will piggyback on the `migrating_from`
            // field to force the coordinator to provide us another SFU
            var joinFailsPerSfu = new Dictionary<string, int>();

            for (int attempt = 0; attempt < CallJoinMaxRetries; attempt++)
            {
                try
                {
                    _lastJoinCallCredentials = null;
                    await DoJoin(joinCallData, cancellationToken);
                    return;
                    //TODO: joinData.ClearMigrationData
                }
                catch (Exception e)
                {
                    _logs.Warning($"Failed to join call `{joinCallData.Id}`, attempt: {attempt}");

                    //TODO: check if error is no recoverable
                    //if (err instanceof ErrorFromResponse && err.unrecoverable)

                    var sfuId = _lastJoinCallCredentials?.Server.EdgeName ?? string.Empty;
                    var sfuFails = joinFailsPerSfu.GetValueOrDefault(sfuId) + 1;

                    if (sfuFails > 2)
                    {
                        joinCallData = joinCallData.CloneWithMigratingFromSfu(sfuId);
                    }

                    if (attempt == CallJoinMaxRetries - 1)
                    {
                        throw;
                    }

                    //StreamTODO: randomize a bit
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        //StreamTODO: cancellation token
        public async Task DoJoin(JoinCallData joinCallData, CancellationToken cancellationToken)
        {
            if (CallState == CallingState.Joining)
            {
                _logs.Error($"{nameof(DoJoin)} called while already joining.");
                throw new NotSupportedException("Already joining");

            }
            var prevCallState = CallState;

            _joinCallData = joinCallData;

            CallState = CallingState.Joining;

            if (_joinCallCts != null)
            {
                _logs.ErrorIfDebug("Previous join call CTS was not cleaned up properly. Cancelling it now.");
                _joinCallCts.Cancel();
                _joinCallCts.Dispose();
                _joinCallCts = null;
            }

            if (_activeCallCts != null)
            {
                _logs.ErrorIfDebug("Previous active call CTS was not cleaned up properly. Cancelling it now.");
                _activeCallCts.Cancel();
                _activeCallCts.Dispose();
                _activeCallCts = null;
            }

            _joinCallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pendingIceTrickleRequests.Clear();

            try
            {
                var isMigration = _reconnectStrategy == WebsocketReconnectStrategy.Migrate;
                var isRejoin = _reconnectStrategy == WebsocketReconnectStrategy.Rejoin;
                var isFast = _reconnectStrategy == WebsocketReconnectStrategy.Fast;

                var callCid = joinCallData.Type + ":" + joinCallData.Id;

                // Call can exist without credentials because it can be added via call.created event
                var callCredentialsExists = _cache.Calls.TryGet(callCid, out var call) && call.Credentials != null;

                if (!callCredentialsExists || isRejoin || isMigration)
                {
                    try
                    {
                        _logs.WarningIfDebug($"{nameof(DoJoin)} - Execute join call API request.");
                        call = await ExecuteJoinRequest(joinCallData, cancellationToken);
                        _lastJoinCallCredentials = call.Credentials;
                    }
                    catch (Exception e)
                    {
                        _logs.ExceptionIfDebug(e);
                        if (CallState != CallingState.Offline)
                        {
                            CallState = prevCallState;
                        }

                        throw;
                    }
                }
                else
                {
                    _logs.WarningIfDebug(
                        $"{nameof(DoJoin)} - Skipped join call request: callExists: {callCredentialsExists}, isRejoin: {isRejoin}, isMigration: {isMigration}");
                }

                ActiveCall = call ?? throw new NullReferenceException(nameof(call));

                if (ActiveCall.Credentials == null || string.IsNullOrEmpty(ActiveCall.Credentials.Token))
                {
                    _logs.ErrorIfDebug($"{nameof(DoJoin)} - Missing credentials!");
                }

                _httpClient = _httpClientFactory(ActiveCall);

                var previousSfuWebSocket = _sfuWebSocket;
                var isWsHealthy = previousSfuWebSocket?.IsHealthy ?? false;
                var startNewSfuWsSession = isRejoin || isMigration || !isWsHealthy;

                // Following JS: Publisher/Subscriber are only recreated for REJOIN/MIGRATE.
                // For FAST reconnect (even with unhealthy WS), we keep existing pub/sub.
                // Also create new ones if they don't exist (initial join).
                var startNewPeerConnections = isRejoin || isMigration || Publisher == null || !Publisher.IsHealthy ||
                                              Subscriber == null || !Subscriber.IsHealthy;

                string previousSessionId = SessionId.ToString();

                // a new session_id is necessary for the REJOIN strategy.
                // we use the previous session_id if available
                if (isRejoin || SessionId.IsEmpty)
                {
                    _logs.WarningIfDebug($"{nameof(DoJoin)} - Regenerate session ID. {nameof(isRejoin)}:{isRejoin}, SessionID is empty: {SessionId.IsEmpty}");
                    SessionId.Regenerate();
                }

                if (startNewSfuWsSession)
                {
                    _logs.WarningIfDebug($"{nameof(DoJoin)} - Create new SFU Session");
                    CreateNewSfuWebSocket(out previousSfuWebSocket);

                    var sfuUrl = call.Credentials.Server.Url;
                    var sfuToken = call.Credentials.Token;
                    var iceServers = call.Credentials.IceServers;

                    // Initialize tracers with the correct ID format - separate tracers for SFU, Publisher, and Subscriber
                    var sfuUrlForId = sfuUrl.Replace("https://", "").Replace("/twirp", "");
                    var sessionNumber = _sessionCounter + 1;
                    _sfuTracer = _tracerManager.GetTracer($"{sessionNumber}-{sfuUrlForId}");
                    _publisherTracer = _tracerManager.GetTracer($"{sessionNumber}-pub");
                    _subscriberTracer = _tracerManager.GetTracer($"{sessionNumber}-sub");
                    _sessionCounter++;

                    if (startNewPeerConnections)
                    {
                        _logs.WarningIfDebug($"{nameof(DoJoin)} - Create new Publisher and Subscriber");
                        
                        // REJOIN/MIGRATE: Create new Publisher/Subscriber
                        CreatePublisher(call.Credentials.IceServers);
                        CreateSubscriber(call.Credentials.IceServers);
                    }
                    else
                    {
                        _logs.WarningIfDebug($"{nameof(DoJoin)} - Don't create new Publisher and Subscriber. Both exist: {(Subscriber != null)}, {(Publisher != null)}");
                    }
                    // else: FAST with unhealthy WS - keep existing Publisher/Subscriber.
                    // No need to update SfuClient reference because Publisher/Subscriber hold a reference to RtcSession
                    // (which implements ISfuClient), and RtcSession internally manages the _sfuWebSocket.

                    // We don't set initial offer as local. Later on we set generated answer as a local
                    var subscriberOffer = await Subscriber.CreateOfferAsync(_joinCallCts.Token);
                    var publisherOffer = await Publisher.CreateOfferAsync(_joinCallCts.Token);

                    _sfuWebSocket.InitNewSession(SessionId.ToString(), sfuUrl, sfuToken, subscriberOffer.sdp, publisherOffer.sdp);

                    var reconnectDetails = new ReconnectDetails
                    {
                        Strategy = _reconnectStrategy,
                        ReconnectAttempt = (uint)_reconnectAttempts,
                        FromSfuId = joinCallData.MigratingFromSfu,
                        PreviousSessionId = previousSessionId,
                        Reason = _reconnectReason ?? string.Empty,
                    };

                    var announcedTracks = Publisher.GetAnnouncedTracksForReconnect();
                    if (announcedTracks?.Any() ?? false)
                    {
#if STREAM_DEBUG_ENABLED
                        _logs.WarningIfDebug("DoJoin - announcedTracks " + string.Join(", ", announcedTracks.Select(t => t.TrackType.ToString())));
#endif
                        reconnectDetails.AnnouncedTracks.AddRange(announcedTracks);
                    }

                    var desiredTracks = GetDesiredTracksDetails();
                    if (desiredTracks.Any())
                    {
                        reconnectDetails.Subscriptions.AddRange(desiredTracks);
                    }

                    var joinRequest = new SfuWebSocket.ConnectRequest
                    {
                        ReconnectDetails = reconnectDetails
                    };

                    _logs.WarningIfDebug($"{nameof(DoJoin)} - SFU Sending join request");
                    var joinResponse = await _sfuWebSocket.ConnectAsync(joinRequest, cancellationToken);
                    
                    //StreamTODO: if we try to rejoin a call with no other participants we'll get error from SFU not call FOUND
                    // What should we do then?
                    
                    
                    ActiveCall.UpdateFromSfu(joinResponse);
                    _logs.WarningIfDebug($"{nameof(DoJoin)} - SFU Sending join response received. startNewPeerConnections: {startNewPeerConnections}");

                    _fastReconnectDeadlineSeconds = joinResponse.FastReconnectDeadlineSeconds;

                    if (startNewPeerConnections)
                    {
                        _logs.WarningIfDebug($"{nameof(DoJoin)} - startNewPeerConnections, _publisherAudioTrackIsEnabled: {_publisherAudioTrackIsEnabled}, _publisherVideoTrackIsEnabled: {_publisherVideoTrackIsEnabled}");
                        // Only init publisher tracks for new Publisher
                        
                        await Publisher.InitPublisherTracksAsync();
                        await NotifyCurrentMuteStatesAsync();

                        // Handle tracks subscriptions for already present participants
                        foreach (var participant in ActiveCall.Participants)
                        {
                            if (!participant.IsLocalParticipant)
                            {
                                NotifyParticipantJoined(participant.SessionId);
                            }
                        }

                        QueueTracksSubscriptionRequest();
                    }
                }
                else
                {
                    _logs.WarningIfDebug($"{nameof(DoJoin)} - Reuse SFU Session");
                }

                if (!isMigration)
                {
                    // in MIGRATION, `JOINED` state is set in `this.reconnectMigrate()`
                    CallState = CallingState.Joined;
                }

                // Following JS: For FAST reconnect, always call RestartIce (after updating SfuClient if needed).
                // The SFU automatically issues an ICE restart on the subscriber, we don't have to do it ourselves.
                if (isFast)
                {
                    _logs.WarningIfDebug($"{nameof(DoJoin)} - Restart ICE");
                    await Publisher.RestartIce(); //StreamTODO: cancellation token
                }

                if (!isRejoin && !isFast && !isMigration)
                {
                    // TODO: send sendConnectionTime   
                }

                if (previousSfuWebSocket != null && previousSfuWebSocket != _sfuWebSocket)
                {
                    var closeReason = isRejoin
                        ? "Closing WS after rejoin"
                        : "Closing unhealthy WS after reconnect";

                    _logs.WarningIfDebug($"{nameof(DoJoin)} - Close previous SFU WS - " + closeReason);
                    ClosePreviousSfuWebSocketAsync(previousSfuWebSocket, closeReason).LogIfFailed();
                }

                //StreamTODO: JS client deletes here ring and notify data because these are one-time actions
                //delete this.joinCallData?.ring;
                //delete this.joinCallData?.notify;

                _reconnectStrategy = WebsocketReconnectStrategy.Unspecified;
                _reconnectReason = string.Empty;

                if (UseNativeAudioBindings)
                {
                    //StreamTODO: Either use UseNativeAudioBindings const or STREAM_NATIVE_AUDIO flag but not both. Once we replace the webRTC package we could remove STREAM_NATIVE_AUDIO
#if STREAM_NATIVE_AUDIO
                    WebRTC.StartAudioPlayback(AudioOutputSampleRate, AudioOutputChannels);
#endif
                }

                _logs.Info($"{nameof(DoJoin)} - Joined call: {call.Cid}, Call State: {CallState}");
            }
            catch (Exception e)
            {
                _logs.Error($"{nameof(DoJoin)} failed with exception: {e.Message}");
                _logs.Exception(e);
                throw;
            }
            finally
            {
                if (_joinCallCts != null)
                {
                    _joinCallCts.Dispose();
                    _joinCallCts = null;
                }
            }
        }

        //StreamTODO: move
        private async Task<StreamCall> ExecuteJoinRequest(JoinCallData data, CancellationToken cancellationToken)
        {
            // StreamTodo: check state if we don't have an active session already
            var locationHint = await _lowLevelClient.GetLocationHintAsync(cancellationToken);

            //StreamTodo: move this logic to call.Join, this way user can create call object and join later on 

            // StreamTodo: expose params
            var joinCallRequest = new JoinCallRequestInternalDTO
            {
                Create = data.Create,
                Data = new CallRequestInternalDTO
                {
                    Custom = null,
                    Members = null,
                    SettingsOverride = null,

                    //StreamTODO: check this, if we're just joining another call perhaps we shouldn't set this?
                    StartsAt = DateTimeOffset.Now,
                    Team = null
                },
                Location = locationHint,
                MembersLimit = 0,
                MigratingFrom = null,
                Notify = data.Notify,
                Ring = data.Ring
            };

            var joinCallResponse
                = await _lowLevelClient.InternalVideoClientApi.JoinCallAsync(data.Type, data.Id, joinCallRequest);
            var streamCall = _cache.TryCreateOrUpdate(joinCallResponse);

            //StreamTODO: add ring accept logic. Check JS doJoinRequest

            return streamCall;
        }

        public async Task StopAsync(string reason = "")
        {
            if (UseNativeAudioBindings)
            {
#if STREAM_NATIVE_AUDIO
                WebRTC.StopAudioPlayback();
#endif
            }

            if (CallState == CallingState.Leaving || CallState == CallingState.Offline)
            {
                _logs.WarningIfDebug($"{nameof(StopAsync)} ignored because call is in state: " + CallState);
                //StreamTODO: should this return a task of the ongoing stop?
                return;
            }

            //StreamTODO: revise this. Right now StopAsync is always called on disconnect, perhaps we can leave it this way
            // if (CallState != CallingState.Joined && CallState != CallingState.Joining)
            // {
            //     throw new InvalidOperationException(
            //         "Tried to leave call that is not joined or joining. Current state: " + CallState);
            // }

            CallState = CallingState.Leaving;
            _logs.InfoIfDebug("Leaving the call - cleanup session");

            if (_joinCallCts != null)
            {
                _joinCallCts.Cancel();
            }

            if (_activeCallCts != null)
            {
                _activeCallCts.Cancel();
            }

            if (ActiveCall != null)
            {
                _logs.Info("Leaving active call with Cid: " + ActiveCall.Cid);
                try
                {
                    // Trace leave call before leaving the call. Otherwise, stats are not send because SFU WS disconnects
                    _sfuTracer?.Trace(PeerConnectionTraceKey.LeaveCall, new { SessionId = SessionId.ToString(), Reason = reason });

                    if (_statsSender != null) // This was null in tests
                    {
                        var sendStatsCancellationToken = new CancellationTokenSource();
                        sendStatsCancellationToken.CancelAfter(800);
                        using (new TimeLogScope("Sending final stats on leave", _logs.Info))
                        {
                            await _statsSender.SendFinalStatsAsync(sendStatsCancellationToken.Token);
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logs.Info($"Network unavailable during final stats send: {httpEx.Message}");
                }
                catch (OperationCanceledException)
                {
                    _logs.Info("Final stats send timed out.");
                }
                catch (Exception e)
                {
                    _logs.Warning($"Failed to send final stats on leave: {e.Message}");
                }
            }

            await ClearSessionAsync();

            if (_sfuWebSocket != null)
            {
                using (new TimeLogScope("Sending leave call request & disconnect", _logs.Info))
                {
                    await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, reason);
                }
            }

            //StreamTodo: check with js definition of "offline" 
            CallState = CallingState.Offline;

#if STREAM_DEBUG_ENABLED
            _videoAudioSyncBenchmark?.Finish();
#endif
        }

        public void UpdateRequestedVideoResolution(string participantSessionId, VideoResolution videoResolution)
        {
            _videoResolutionByParticipantSessionId[participantSessionId] = videoResolution;
            QueueTracksSubscriptionRequest();
        }

        public void UpdateIncomingVideoRequested(string participantSessionId, bool isRequested)
        {
            _incomingVideoRequestedByParticipantSessionId[participantSessionId] = isRequested;
            QueueTracksSubscriptionRequest();
        }

        public void UpdateIncomingAudioRequested(string participantSessionId, bool isRequested)
        {
            _incomingAudioRequestedByParticipantSessionId[participantSessionId] = isRequested;
            QueueTracksSubscriptionRequest();
        }

        // Let's request video for the first 10 participants that join
        public void NotifyParticipantJoined(string participantSessionId)
        {
            if (ActiveCall == null)
            {
                return;
            }

            var participantCount = ActiveCall.Participants?.Count ?? 0;
            var requestVideo = participantCount <= MaxParticipantsForVideoAutoSubscription;
            var requestAudio = true; // No limit by default

            _incomingVideoRequestedByParticipantSessionId.TryAdd(participantSessionId, requestVideo);
            _incomingAudioRequestedByParticipantSessionId.TryAdd(participantSessionId, requestAudio);
        }

        public void NotifyParticipantLeft(string participantSessionId)
        {
            _videoResolutionByParticipantSessionId.Remove(participantSessionId);
            _incomingVideoRequestedByParticipantSessionId.Remove(participantSessionId);
            _incomingAudioRequestedByParticipantSessionId.Remove(participantSessionId);
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

        //StreamTODO: temp solution to allow stopping the audio when app is minimized. User tried disabling the AudioSource but the audio is handled natively so it has no effect
        public void PauseAndroidAudioPlayback()
        {
#if STREAM_NATIVE_AUDIO
            WebRTC.MuteAndroidAudioPlayback();
            _logs.Warning("Audio Playback is paused. This stops all audio coming from StreamVideo SDK on Android platform.");
#else
            throw new NotSupportedException(
                $"{nameof(PauseAndroidAudioPlayback)} is only supported on Android platform.");
#endif
        }

        //StreamTODO: temp solution to allow stopping the audio when app is minimized. User tried disabling the AudioSource but the audio is handled natively so it has no effect
        public void ResumeAndroidAudioPlayback()
        {
#if STREAM_NATIVE_AUDIO
            WebRTC.UnmuteAndroidAudioPlayback();
            _logs.Warning("Audio Playback is resumed. This resumes audio coming from StreamVideo SDK on Android platform.");
#else
            throw new NotSupportedException(
                $"{nameof(ResumeAndroidAudioPlayback)} is only supported on Android platform.");
#endif
        }

        /// <summary>
        /// Set Publisher Video track enabled/disabled, if track is available, or store the preference for when track becomes available
        /// </summary>
        public void TrySetPublisherVideoTrackEnabled(bool isEnabled) => PublisherVideoTrackIsEnabled = isEnabled;

        private const float TrackSubscriptionDebounceTime = 0.1f;
        private const int CallJoinMaxRetries = 3;
        private const int CallRejoinMaxFastAttempts = 3;

        private readonly ISfuWebSocketFactory _sfuWebSocketFactory;
        private SfuWebSocket _sfuWebSocket;
        private readonly ISerializer _serializer;
        private readonly ILogs _logs;
        private readonly ITimeService _timeService;
        private readonly IStreamClientConfig _config;
        private readonly Func<IStreamCall, HttpClient> _httpClientFactory;
        private readonly WebRtcStatsSender _statsSender;
        private readonly VideoAudioSyncBenchmark _videoAudioSyncBenchmark;
        private readonly SdpMungeUtils _sdpMungeUtils = new SdpMungeUtils();
        private readonly TracerManager _tracerManager = new TracerManager(enabled: true);
        private readonly StreamVideoLowLevelClient _lowLevelClient;
        private readonly INetworkMonitor _networkMonitor;

        private Tracer _sfuTracer;
        private Tracer _publisherTracer;
        private Tracer _subscriberTracer;

        private readonly List<SfuICETrickle> _pendingIceTrickleRequests = new List<SfuICETrickle>();
        private readonly PublisherVideoSettings _publisherVideoSettings = PublisherVideoSettings.Default;

        private readonly Dictionary<string, VideoResolution> _videoResolutionByParticipantSessionId
            = new Dictionary<string, VideoResolution>();

        private readonly Dictionary<string, bool> _incomingVideoRequestedByParticipantSessionId
            = new Dictionary<string, bool>();

        private readonly Dictionary<string, bool> _incomingAudioRequestedByParticipantSessionId
            = new Dictionary<string, bool>();

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

        private CancellationTokenSource _joinCallCts;
        private CancellationTokenSource _activeCallCts;
        
        /// <summary>
        /// Flag to track if a reconnection is in progress. This prevents parallel reconnection
        /// attempts from both Publisher and Subscriber peer connections.
        /// </summary>
        private bool _isReconnecting;

        private TaskCompletionSource<bool> _joinTaskCompletionSource;
        private int _fastReconnectDeadlineSeconds;

        private WebsocketReconnectStrategy _reconnectStrategy = WebsocketReconnectStrategy.Unspecified;
        private string _reconnectReason;
        private int _reconnectAttempts;
        private JoinCallData _joinCallData;

        private Credentials _lastJoinCallCredentials;
        private DateTime _lastTimeOffline;
        private int _mainThreadId;

        private async Task ClearSessionAsync()
        {
            if (_sfuWebSocket != null)
            {
                UnsubscribeFromSfuEvents(_sfuWebSocket);
                await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, "Clearing current session");
                _sfuWebSocket.Dispose();
                _sfuWebSocket = null;
            }

            _pendingIceTrickleRequests.Clear();
            _videoResolutionByParticipantSessionId.Clear();
            _incomingVideoRequestedByParticipantSessionId.Clear();
            _incomingAudioRequestedByParticipantSessionId.Clear();
            _tracerManager?.Clear();

            Subscriber?.Dispose();
            Subscriber = null;
            Publisher?.Dispose();
            Publisher = null;

            ActiveCall = null;
            CallState = CallingState.Unknown;
            _httpClient = null;

            if (_joinCallCts != null)
            {
                _joinCallCts.Dispose();
                _joinCallCts = null;
            }

            if (_activeCallCts != null)
            {
                _activeCallCts.Dispose();
                _activeCallCts = null;
            }

            _trackSubscriptionRequested = false;
            _trackSubscriptionRequestInProgress = false;

            SessionId.Clear();
        }

        private CancellationToken GetCurrentCancellationTokenOrDefault()
        {
            if (_activeCallCts != null)
            {
                return _activeCallCts.Token;
            }

            return _joinCallCts?.Token ?? default;
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

            SubscribeToTracksAsync(GetCurrentCancellationTokenOrDefault()).ContinueWith(t =>
            {
                if (ActiveCall == null)
                {
                    return;
                }

                t.LogIfFailed();
            });

            _lastTrackSubscriptionRequestTime = _timeService.Time;
            _trackSubscriptionRequested = false;
        }

        /// <summary>
        /// Request this via <see cref="QueueTracksSubscriptionRequest"/>. We don't want to call it too often
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async Task SubscribeToTracksAsync(CancellationToken cancellationToken)
        {
            if (ActiveCall?.Participants == null || !ActiveCall.Participants.Any())
            {
#if STREAM_DEBUG_ENABLED
                _logs.Error(
                    $"{nameof(SubscribeToTracksAsync)} Ignored - No participants in the call to subscribe tracks for");
#endif

                return;
            }

            if (_trackSubscriptionRequestInProgress)
            {
                QueueTracksSubscriptionRequest();
                return;
            }

            _trackSubscriptionRequestInProgress = true;

            // StreamTodo: validate that the very first call to SubscribeToTracksAsync is correct because ActiveCall.Participants may not have been updated yet
            var tracks = GetDesiredTracksDetails();

            var request = new UpdateSubscriptionsRequest
            {
                SessionId = SessionId.ToString(),
            };
            request.Tracks.AddRange(tracks);

#if STREAM_DEBUG_ENABLED
            _logs.Info($"Request SFU - UpdateSubscriptionsRequest\n{_serializer.Serialize(request)}");
#endif

            var response = await RpcCallAsync(request, GeneratedAPI.UpdateSubscriptions,
                nameof(GeneratedAPI.UpdateSubscriptions), cancellationToken, response => response.Error);

            if (ActiveCall == null)
            {
                //Ignore if call ended during this request
                return;
            }

            if (response?.Error != null)
            {
                _logs.Error(response.Error.Message);
            }

            _trackSubscriptionRequestInProgress = false;
        }

        private IEnumerable<TrackSubscriptionDetails> GetDesiredTracksDetails()
        {
            if (ActiveCall.Participants == null || ActiveCall.Participants.Count == 0)
            {
                var count = ActiveCall.Participants?.Count.ToString() ?? "null";
                _logs.WarningIfDebug($"{nameof(GetDesiredTracksDetails)} participants null or empty. Participants: " + count);
                yield break;
            }
            //StreamTodo: inject info on what tracks we want. Hardcoded audio & video but missing screenshare support
            var trackTypes = new[] { SfuTrackType.Video, SfuTrackType.Audio };

            foreach (var participant in ActiveCall.Participants)
            {
                if (participant == null)
                {
                    _logs.Error("Cannot subscribe to tracks - participant is null");
                    continue;
                }

                if (participant.IsLocalParticipant)
                {
                    continue;
                }

                var userId = GetUserId(participant);
                if (string.IsNullOrEmpty(userId))
                {
                    _logs.Error(
                        $"Cannot subscribe to any tracks - participant UserId is null or empty. SessionID: {participant.SessionId}");
                    continue;
                }

                var shouldConsumeAudio = ShouldSubscribeToAudioTrack(participant);
                if (shouldConsumeAudio)
                {
                    yield return new TrackSubscriptionDetails
                    {
                        UserId = userId,
                        SessionId = participant.SessionId,
                        TrackType = SfuTrackType.Audio,
                    };
                }

                var shouldConsumeVideo = ShouldSubscribeToVideoTrack(participant);
                if (shouldConsumeVideo)
                {
                    var requestedVideoResolution = GetRequestedVideoResolution(participant);

                    yield return new TrackSubscriptionDetails
                    {
                        UserId = userId,
                        SessionId = participant.SessionId,
                        TrackType = SfuTrackType.Video,
                        Dimension = requestedVideoResolution.ToVideoDimension()
                    };
                }
            }
        }

        private bool ShouldSubscribeToVideoTrack(IStreamVideoCallParticipant participant)
            => _incomingVideoRequestedByParticipantSessionId.GetValueOrDefault(participant.SessionId, false);

        private bool ShouldSubscribeToAudioTrack(IStreamVideoCallParticipant participant)
            => _incomingAudioRequestedByParticipantSessionId.GetValueOrDefault(participant.SessionId, false);

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
                    SessionId = SessionId.ToString(),
                };

                if (_callState == CallingState.Joined)
                {
                    var cancellationToken = GetCurrentCancellationTokenOrDefault();
                    await RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle),
                        cancellationToken, response => response.Error);
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
                _logs.WarningIfDebug($"RtcSession.UpdateAudioRecording -> IGNORE because publisher ({Publisher == null}) or audio track ({Publisher?.PublisherAudioTrack == null}) are null");
                return;
            }

#if STREAM_NATIVE_AUDIO
            var shouldRecord = _activeAudioRecordingDevice.IsValid && _publisherAudioTrackIsEnabled;

            if (shouldRecord)
            {
                //StreamTODO: implement proper passing deviceID -> for Android and IOS we're skipping the deviceID
                //because they operate on audio routing instead of actual devices. The underlying native implementation for Android let's OS pick the preferred device

                _logs.WarningIfDebug("RtcSession.UpdateAudioRecording -> START local audio capture");
                Publisher.PublisherAudioTrack.StartLocalAudioCapture(-1, AudioInputSampleRate);
            }
            else
            {
                _logs.WarningIfDebug("RtcSession.UpdateAudioRecording -> STOP local audio capture");
                Publisher.PublisherAudioTrack.StopLocalAudioCapture();
            }
#endif
        }

        private void OnSfuJoinResponse(JoinResponse joinResponse)
        {
            //StreamTODO: what if left the call and started a new one but the JoinResponse belongs to the previous session?

            _sfuTracer?.Trace(PeerConnectionTraceKey.JoinRequest, joinResponse);

            // State update was already handled in DoJoin

            // Not sure if still needed but as a safe net we flush any pending ice candidates
            var cancellationToken = GetCurrentCancellationTokenOrDefault();
            foreach (var iceTrickle in _pendingIceTrickleRequests)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                RpcCallAsync(iceTrickle, GeneratedAPI.IceTrickle, nameof(GeneratedAPI.IceTrickle),
                    cancellationToken, response => response.Error).LogIfFailed();
            }
            
            _pendingIceTrickleRequests.Clear();
        }

        private void OnSfuIceTrickle(SfuICETrickle iceTrickle)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.AddIceCandidate, iceTrickle);

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
            _sfuTracer?.Trace(PeerConnectionTraceKey.SetRemoteDescription, subscriberOffer);

#if STREAM_DEBUG_ENABLED
            _logs.Warning("OnSfuSubscriberOffer");
#endif
            //StreamTodo: check RtcSession.kt handleSubscriberOffer for the retry logic

            try
            {
                if (GetCurrentCancellationTokenOrDefault().IsCancellationRequested)
                {
                    return;
                }

                //StreamTodo: handle subscriberOffer.iceRestart
                var rtcSessionDescription = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = subscriberOffer.Sdp
                };

                try
                {
                    await Subscriber.SetRemoteDescriptionAsync(rtcSessionDescription,
                        GetCurrentCancellationTokenOrDefault());
                    Subscriber.ThrowDisposedDuringOperationIfNull();

                    Subscriber.AddPendingIceCandidates();
                }
                catch (Exception e)
                {
                    _subscriberTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSetRemoteDescription,
                        e.Message ?? "unknown");
                    throw;
                }

                var answer = await Subscriber.CreateAnswerAsync(GetCurrentCancellationTokenOrDefault());
                Subscriber.ThrowDisposedDuringOperationIfNull();

                //StreamTodo: mangle SDP

                try
                {
                    await Subscriber.SetLocalDescriptionAsync(ref answer, GetCurrentCancellationTokenOrDefault());
                    Subscriber.ThrowDisposedDuringOperationIfNull();
                }
                catch (Exception e)
                {
                    _subscriberTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSetLocalDescription,
                        e.Message ?? "unknown");
                    throw;
                }

                var sendAnswerRequest = new SendAnswerRequest
                {
                    PeerType = PeerType.Subscriber,
                    Sdp = answer.sdp,
                    SessionId = SessionId.ToString()
                };

                await RpcCallAsync(sendAnswerRequest, GeneratedAPI.SendAnswer, nameof(GeneratedAPI.SendAnswer),
                    GetCurrentCancellationTokenOrDefault(), response => response.Error, preLog: true);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown, don't log as error
            }
            catch (DisposedDuringOperationException)
            {
                // Expected during shutdown
            }
            catch (Exception e)
            {
                _subscriberTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSubmit, e.Message ?? "unknown");
                _logs.Exception(e);
            }
        }

        private void OnSfuTrackUnpublished(TrackUnpublished trackUnpublished)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.OnTrack, trackUnpublished);

            var userId = trackUnpublished.UserId;
            var sessionId = trackUnpublished.SessionId;
            var type = trackUnpublished.Type.ToPublicEnum();
            var cause = trackUnpublished.Cause;

            // StreamTODO: test if this works well with other user muting this user
            var updateLocalParticipantState
                = cause != TrackUnpublishReason.Unspecified && cause != TrackUnpublishReason.UserMuted;

            // Optionally available. Read TrackUnpublished.participant comment in events.proto
            var participantSfuDto = trackUnpublished.Participant;

            UpdateParticipantTracksState(userId, sessionId, type, isEnabled: false, updateLocalParticipantState,
                out var participant);

            if (participantSfuDto != null && participant != null)
            {
                participant.UpdateFromSfu(participantSfuDto);
            }

            //StreamTodo: raise an event so user can react to track unpublished? Otherwise the video will just freeze
        }

        private void OnSfuTrackPublished(TrackPublished trackPublished)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.OnTrack, trackPublished);

            var userId = trackPublished.UserId;
            var sessionId = trackPublished.SessionId;
            var type = trackPublished.Type.ToPublicEnum();

            // Optionally available. Read TrackUnpublished.participant comment in events.proto
            var participantSfuDto = trackPublished.Participant;

            UpdateParticipantTracksState(userId, sessionId, type, isEnabled: true, updateLocalParticipantState: true,
                out var participant);

            if (participantSfuDto != null && participant != null)
            {
                participant.UpdateFromSfu(participantSfuDto);
            }

            //StreamTodo: fixes the case when joining a call where other participant starts with no video and activates video track after we've joined -
            // validated that this how Android/Js is handling this
            QueueTracksSubscriptionRequest();
        }

        private void UpdateParticipantTracksState(string userId, string sessionId, TrackType trackType, bool isEnabled,
            bool updateLocalParticipantState, out StreamVideoCallParticipant participant)
        {
            participant = (StreamVideoCallParticipant)ActiveCall.Participants.FirstOrDefault(p
                => p.SessionId == sessionId);
            if (participant == null)
            {
                // This seems to be a valid case. When other participant joins we may receive TrackPublished event before we manage to subscribe for it
                return;
            }

            if (participant.IsLocalParticipant && updateLocalParticipantState)
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

            participant.NotifyTrackEnabled(trackType, isEnabled);

            ActiveCall.NotifyTrackStateChanged(participant, trackType, isEnabled);
        }

        private async void OnReconnectionNeeded(WebsocketReconnectStrategy strategy, string reason,
            StreamPeerType peerType)
        {
            try
            {
                _logs.WarningIfDebug($"Reconnect triggered by {nameof(OnReconnectionNeeded)}");
                await Reconnect(strategy, reason);
            }
            catch (Exception e)
            {
                //Js logs reconnect errors as warning only
                _logs.Warning($"[Reconnect] Reconnection error from {peerType}, Reason: `{reason}`. Error: `{e}`");
            }
        }

        private (WebsocketReconnectStrategy strategy, string reason)? _pendingReconnectRequest;

        private void TryExecutePendingReconnectRequest()
        {
            var pendingRequest = _pendingReconnectRequest;
            _pendingReconnectRequest = default;

            if (pendingRequest.HasValue)
            {
                Reconnect(pendingRequest.Value.strategy, pendingRequest.Value.reason).LogIfFailed();
            }
        }

        //StreamTODO: add triggering from SFU WS closed.
        // In JS -> Call.ts -> onSignalClose -> handleSfuSignalClose -> reconnect
        //StreamTODO: add triggering from network changed -> js Call.ts "network.changed"
        private async Task Reconnect(WebsocketReconnectStrategy strategy, string reason)
        {
            if (!AssertMainThread())
            {
                _pendingReconnectRequest = new ValueTuple<WebsocketReconnectStrategy, string>(strategy, reason);
                return;
            }

            // Ignore reconnection requests if we're already reconnecting, migrating, or joining
            // This prevents parallel reconnection attempts from both Publisher and Subscriber
            var ignoredStates = new[]
            {
                CallingState.Reconnecting, CallingState.Migrating, CallingState.Joining,
                CallingState.Offline, CallingState.Leaving, CallingState.Left
            };
            if (ignoredStates.Any(s => s == CallState))
            {
                _logs.WarningIfDebug($"[Reconnect] Ignoring reconnect request because CallState is {CallState}");
                return;
            }
            
            // Use a flag to track if we're in the process of reconnecting
            // This protects against race conditions before CallState is updated
            if (_isReconnecting)
            {
                _logs.WarningIfDebug($"[Reconnect] Ignoring reconnect request because reconnection is already in progress");
                return;
            }
            
            _isReconnecting = true;
            _logs.WarningIfDebug($"--------- Reconnection FLOW TRIGGERED ---------- strategy: {strategy}, reason: {reason}");

            try
            {
                _reconnectStrategy = strategy;
                _reconnectReason = reason;

                var finishedStates = new[] { CallingState.Joined, CallingState.ReconnectingFailed, CallingState.Left };

                // Try to get the latest state from the server. State might have changed while we were offline
                try
                {
                    var getCallResponse
                        = await _lowLevelClient.InternalVideoClientApi.GetCallAsync(ActiveCall.Type, ActiveCall.Id,
                            new GetOrCreateCallRequestInternalDTO(), GetCurrentCancellationTokenOrDefault());
                    _cache.TryCreateOrUpdate(getCallResponse);
                }
                catch (Exception e)
                {
                    _logs.ExceptionIfDebug(e);
                    CallState = CallingState.ReconnectingFailed;
                    throw;
                }

                var attempt = 0;
                var reconnectStartTime = DateTime.UtcNow;

                //StreamTODO: we should handle cancellation token between each await

                do
                {
                    // StreamTODO: consider give up timeout. JS has it

                    // Only increment attempts if the strategy is not FAST
                    if (_reconnectStrategy != WebsocketReconnectStrategy.Fast)
                    {
                        _reconnectAttempts++;
                    }

                    try
                    {
                        _logs.Info("Reconnect with strategy: " + _reconnectStrategy);

                        switch (_reconnectStrategy)
                        {
                            case WebsocketReconnectStrategy.Unspecified:
                            case WebsocketReconnectStrategy.Disconnect:

                                // Log warning

                                break;
                            case WebsocketReconnectStrategy.Fast:
                                await ReconnectFast();
                                break;
                            case WebsocketReconnectStrategy.Rejoin:
                                await ReconnectRejoin();
                                break;
                            case WebsocketReconnectStrategy.Migrate:
                                await ReconnectMigrate();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception e)
                    {
                        _logs.ExceptionIfDebug(e);
                        if (e is StreamApiException apiException && apiException.Unrecoverable)
                        {
                            _logs.Error("Can't reconnect due to coordinator unrecoverable error: " + apiException);
                            throw;
                        }

                        await Task.Delay(500, GetCurrentCancellationTokenOrDefault());

                        var wasMigrating = _reconnectStrategy == WebsocketReconnectStrategy.Migrate;

                        var fastReconnectTimeout = (DateTime.UtcNow - reconnectStartTime).TotalSeconds >
                                                   _fastReconnectDeadlineSeconds;

                        var arePeerConnectionsHealthy = (Publisher?.IsHealthy ?? false) && (Subscriber?.IsHealthy ?? false);

                        // don't immediately switch to the REJOIN strategy, but instead attempt
                        // to reconnect with the FAST strategy for a few times before switching.
                        // in some cases, we immediately switch to the REJOIN strategy.
                        var shouldRejoin = fastReconnectTimeout || wasMigrating || attempt >= CallRejoinMaxFastAttempts ||
                                           !arePeerConnectionsHealthy;

                        attempt++;
                        _reconnectStrategy
                            = shouldRejoin ? WebsocketReconnectStrategy.Rejoin : WebsocketReconnectStrategy.Fast;

                        _logs.WarningIfDebug(
                            $"Reconnect failed, attempt: {attempt}, next strategy: {_reconnectStrategy}, wasMigrating: {wasMigrating}, " +
                            $"fastReconnectTimeout: {fastReconnectTimeout}, arePeerConnectionsHealthy: {arePeerConnectionsHealthy}");

                        //StreamTODO: handle cancellation token
                    }
                } while (finishedStates.All(s => s != CallState));
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        private async Task ReconnectFast()
        {
            _reconnectStrategy = WebsocketReconnectStrategy.Fast;
            CallState = CallingState.Reconnecting;
            await DoJoin(_joinCallData, GetCurrentCancellationTokenOrDefault());

            // Refresh state, it might have changed while we were shortly offline 
            var getCallResponse
                = await _lowLevelClient.InternalVideoClientApi.GetCallAsync(ActiveCall.Type, ActiveCall.Id,
                    new GetOrCreateCallRequestInternalDTO(), GetCurrentCancellationTokenOrDefault());
            _cache.TryCreateOrUpdate(getCallResponse);

            //StreamTODO: send reconnection time
            //this.sfuStatsReporter?.sendReconnectionTime(
            //    WebsocketReconnectStrategy.FAST,
            //    (Date.now() - reconnectStartTime) / 1000,
            //);
        }

        private async Task ReconnectRejoin()
        {
            _reconnectStrategy = WebsocketReconnectStrategy.Rejoin;
            CallState = CallingState.Reconnecting;
            await DoJoin(_joinCallData, GetCurrentCancellationTokenOrDefault());

            RestorePublishedTracks();
            RestoreSubscribedTracks();
        }

        private Task ReconnectMigrate()
        {
            throw new NotImplementedException("Sfu migration is not yet implemented.");
        }

        private void RestorePublishedTracks()
        {
            // the tracks need to be restored in their original order of publishing
            // otherwise, we might get `m-lines order mismatch` errors

            foreach (var type in Publisher.PublishedTrackOrder)
            {
                switch (type)
                {
                    case SfuTrackType.Unspecified:
                        break;
                    case SfuTrackType.Audio:
                        TrySetPublisherAudioTrackEnabled(true);
                        break;
                    case SfuTrackType.Video:
                        TrySetPublisherVideoTrackEnabled(true);
                        break;
                    case SfuTrackType.ScreenShare:
                        // Skipped, not supported yet
                        break;
                    case SfuTrackType.ScreenShareAudio:
                        // Skipped, not supported yet
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //StreamTODO: revise this, the JS client executes this a bit differently
        }

        private void RestoreSubscribedTracks()
        {
            TryExecuteSubscribeToTracks();
        }

        private async Task UpdateMuteStateAsync(TrackType trackType, bool isEnabled)
        {
            if (ActiveCall == null)
            {
                return;
            }

            var cancellationToken = _joinCallCts?.Token ?? default;
            await RpcCallAsync(new UpdateMuteStatesRequest
                {
                    SessionId = SessionId.ToString(),
                    MuteStates =
                    {
                        new TrackMuteState
                        {
                            TrackType = trackType.ToInternalEnum(),
                            Muted = !isEnabled
                        }
                    }
                }, GeneratedAPI.UpdateMuteStates, nameof(GeneratedAPI.UpdateMuteStates), cancellationToken,
                response => response.Error);
        }

        private async Task NotifyCurrentMuteStatesAsync()
        {
            if (ActiveCall == null)
            {
                return;
            }

            //StreamTODO: combine into single API call
            if (_publisherAudioTrackIsEnabled)
            {
                await UpdateMuteStateAsync(TrackType.Audio, PublisherAudioTrackIsEnabled);
            }

            if (_publisherVideoTrackIsEnabled)
            {
                await UpdateMuteStateAsync(TrackType.Video, PublisherVideoTrackIsEnabled);
            }
        }

        private void InternalExecuteSetPublisherAudioTrackEnabled(bool isEnabled)
        {
            if (Publisher?.PublisherAudioTrack == null)
            {
                _logs.WarningIfDebug("[Audio] RtcSession.InternalExecuteSetPublisherAudioTrackEnabled isEnabled: " +
                                     isEnabled + " -> track not available yet");
                return;
            }

            _logs.WarningIfDebug("[Audio] RtcSession.InternalExecuteSetPublisherAudioTrackEnabled isEnabled: " +
                                 isEnabled);

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
            _sfuTracer?.Trace("participantJoined", participantJoined);

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
            _sfuTracer?.Trace("participantLeft", participantLeft);

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
            _sfuTracer?.Trace("dominantSpeakerChanged", dominantSpeakerChanged);
            ActiveCall.UpdateFromSfu(dominantSpeakerChanged, _cache);
        }

        private void OnSfuWebSocketOnError(SfuError sfuError)
        {
            AssertMainThread();

            _sfuTracer?.Trace(PeerConnectionTraceKey.SfuError, sfuError);

            var reconnectionStrategy = sfuError.ReconnectStrategy;

            _logs.WarningIfDebug(
                $"Sfu Error - Code: {sfuError.Error_.Code}, Message: {sfuError.Error_.Message}, ShouldRetry: {sfuError.Error_.ShouldRetry}");

            switch (reconnectionStrategy)
            {
                case WebsocketReconnectStrategy.Unspecified:
                    break;
                case WebsocketReconnectStrategy.Disconnect:

                    //StreamTODO: leve the call

                    break;
                case WebsocketReconnectStrategy.Fast:
                case WebsocketReconnectStrategy.Rejoin:
                case WebsocketReconnectStrategy.Migrate:
                    
                    _logs.WarningIfDebug($"Reconnect triggered by {nameof(OnSfuWebSocketOnError)}");

                    //StreamTODO: should this be awaited an this method should be async void?
                    var reason
                        = $"SFU Error -> msg: {sfuError.Error_.Message}, code: {sfuError.Error_.Code}, should retry: {sfuError.Error_.ShouldRetry}";
                    Reconnect(reconnectionStrategy, reason).LogIfFailed();

                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Unhandled {nameof(sfuError)} case: `{reconnectionStrategy}` in {nameof(OnSfuWebSocketOnError)}");
            }
        }

        private void OnSfuPinsUpdated(PinsChanged pinsChanged)
        {
            _sfuTracer?.Trace("pinsChanged", pinsChanged);
            ActiveCall.UpdateFromSfu(pinsChanged, _cache);
        }

        private void OnSfuHealthCheck(HealthCheckResponse healthCheckResponse)
        {
            _sfuTracer?.Trace("healthCheck", healthCheckResponse);
            ActiveCall.UpdateFromSfu(healthCheckResponse, _cache);
        }

        private void OnSfuIceRestart(ICERestart iceRestart)
        {
            _sfuTracer?.Trace("iceRestart", iceRestart);
            // StreamTODO: Implement OnSfuIceRestart
        }

        private void OnSfuGoAway(GoAway goAway)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.GoAway, goAway);
            // StreamTODO: Implement OnSfuGoAway
        }

        private void OnSfuCallGrantsUpdated(CallGrantsUpdated callGrantsUpdated)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.CallEnded, callGrantsUpdated);
            // StreamTODO: Implement OnSfuCallGrantsUpdated
        }

        private void OnSfuChangePublishQuality(ChangePublishQuality changePublishQuality)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.ChangePublishQuality, changePublishQuality);
            // StreamTODO: Implement OnSfuChangePublishQuality
        }

        private void OnSfuConnectionQualityChanged(ConnectionQualityChanged connectionQualityChanged)
        {
            _sfuTracer?.Trace("connectionQualityChanged", connectionQualityChanged);
            // StreamTODO: Implement OnSfuConnectionQualityChanged
        }

        private void OnSfuAudioLevelChanged(AudioLevelChanged audioLevelChanged)
        {
            ActiveCall?.UpdateFromSfu(audioLevelChanged);
        }

        private void OnSfuPublisherAnswer(PublisherAnswer publisherAnswer)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.CreateAnswer, publisherAnswer);
            // StreamTODO: Implement OnSfuPublisherAnswer
        }

        private void OnSfuWebSocketOnChangePublishOptions(ChangePublishOptions obj)
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.ChangePublishOptions, obj);
            // StreamTODO: Implement OnSfuWebSocketOnChangePublishOptions
        }

        private void OnSfuInboundStateNotification(InboundStateNotification obj)
        {
            _sfuTracer?.Trace("inboundStateNotification", obj);
            //StreamTODO: implement
        }

        private void OnSfuWebSocketOnParticipantMigrationComplete()
        {
            _sfuTracer?.Trace("participantMigrationComplete", null);
            // StreamTODO: Implement OnSfuWebSocketOnParticipantMigrationComplete
        }

        private void OnSfuWebSocketOnParticipantUpdated(ParticipantUpdated obj)
        {
            _sfuTracer?.Trace("participantUpdated", obj);
            // StreamTODO: Implement OnSfuWebSocketOnParticipantUpdated
        }

        private void OnSfuWebSocketOnCallEnded()
        {
            _sfuTracer?.Trace(PeerConnectionTraceKey.CallEnded, null);
            // StreamTODO: Implement OnSfuWebSocketOnCallEnded
        }

        private Task<TResponse> RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, CancellationToken, Task<TResponse>> rpcCallAsync, string debugRequestName,
            CancellationToken cancellationToken, Func<TResponse, StreamVideo.v1.Sfu.Models.Error> getError,
            bool preLog = false, bool postLog = true)
            => ((ISfuClient)this).RpcCallAsync(request, rpcCallAsync, debugRequestName, cancellationToken, getError,
                preLog, postLog);

        //StreamTodo: implement retry strategy like in Android SDK
        //If possible, take into account if we the update is still valid e.g. 
        async Task<TResponse> ISfuClient.RpcCallAsync<TRequest, TResponse>(TRequest request,
            Func<HttpClient, TRequest, CancellationToken, Task<TResponse>> rpcCallAsync, string debugRequestName,
            CancellationToken cancellationToken, Func<TResponse, StreamVideo.v1.Sfu.Models.Error> getError,
            bool preLog, bool postLog)
        {
            //StreamTodo: use rpcCallAsync.GetMethodInfo().Name; instead debugRequestName

            if (_httpClient == null)
            {
                var errorMsg
                    = $"[RPC Call: {debugRequestName}] Failed - Attempted to execute RPC call but HttpClient is not yet initialized. " +
                      $"CallState: {CallState}, ActiveCall: {ActiveCall != null}, SessionId: {SessionId}";
                _logs.Error(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            var skipTracing = debugRequestName == nameof(GeneratedAPI.SendStats);

            // Trace the RPC request (except SendStats to avoid noise)
            if (!skipTracing)
            {
                _sfuTracer?.Trace(GetRpcTraceName(debugRequestName), request?.ToString());
            }

#if STREAM_DEBUG_ENABLED
            var serializedRequest = _serializer.Serialize(request);
            if (preLog)
            {
                _logs.Warning($"[RPC REQUEST START] {debugRequestName} {serializedRequest}");
            }
#endif

            // StreamTODO: Add multiple retries
            var response = await rpcCallAsync(_httpClient, request, cancellationToken);

            if (!skipTracing)
            {
                var error = getError(response);
                if (error != null)
                {
                    _sfuTracer?.Trace($"{GetRpcTraceName(debugRequestName)}-error", error.Message);
                }
            }

#if STREAM_DEBUG_ENABLED
            if (postLog)
            {
                var serializedResponse = _serializer.Serialize(response);

                //StreamTodo: move to debug helper class
                var sb = new System.Text.StringBuilder();
                var error = getError(response);
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

        /// <summary>
        /// Converts RPC method name to trace-friendly format (e.g., "SetPublisher" -> "setPublisher")
        /// </summary>
        private string GetRpcTraceName(string debugRequestName)
        {
            if (string.IsNullOrEmpty(debugRequestName))
            {
                return debugRequestName;
            }

            // Convert from PascalCase to camelCase to match Android SDK trace naming
            return char.ToLowerInvariant(debugRequestName[0]) + debugRequestName.Substring(1);
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
            //Debug.LogWarning("OnPublisherNegotiationNeeded. IGNORED.");
#endif

            return;

            try
            {
                if (Publisher.SignalingState != RTCSignalingState.Stable)
                {
                    _logs.Error(
                        $"{nameof(Publisher.SignalingState)} state is not stable, current state: {Publisher.SignalingState}");
                }

                if (GetCurrentCancellationTokenOrDefault().IsCancellationRequested)
                {
                    return;
                }

                var offer = await Publisher.CreateOfferAsync(GetCurrentCancellationTokenOrDefault());
                Publisher.ThrowDisposedDuringOperationIfNull();

                //StreamTOodo: check if SDP is null or empty (this would throw an exception during setting)

                //StreamTodo: ignored the _config.Audio.EnableRed because with current webRTC version this modification causes a crash
                //We're also forcing the red codec in the StreamPeerConnection but atm this results in "InvalidModification"
                //This is most likely issue with the webRTC lib
                if (_config.Audio.EnableDtx)
                {
                    _logs.Error(
                        $"The {nameof(IStreamAudioConfig)} option {nameof(IStreamAudioConfig.EnableDtx)} is temporarily disabled and was ignored. " +
                        $"This error only notifies that this particular setting does not have any effect currently. Send a support ticket if you need this feature.");
                    // offer = new RTCSessionDescription()
                    // {
                    //     type = offer.type,
                    //     sdp = _sdpMungeUtils.ModifySdp(offer.sdp, enableRed: false, _config.Audio.EnableDtx)
                    // };
                    //
                    // _logs.Info(
                    //     $"Modified SDP, enable red: {_config.Audio.EnableRed}, enable DTX: {_config.Audio.EnableDtx} ");
                }

                try
                {
                    await Publisher.SetLocalDescriptionAsync(ref offer, GetCurrentCancellationTokenOrDefault());
                    Publisher.ThrowDisposedDuringOperationIfNull();
                }
                catch (Exception e)
                {
                    _publisherTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSetLocalDescription,
                        e.Message ?? "unknown");
                    throw;
                }

                // //StreamTodo: timeout + break if we're disconnecting/reconnecting
                // while (_sfuWebSocket.ConnectionState != ConnectionState.Connected)
                // {
                //     await Task.Delay(1);
                // }

#if STREAM_DEBUG_ENABLED
                _logs.Warning($"[Publisher] LocalDesc (SDP Offer):\n{offer.sdp}");
#endif

                var tracks = GetPublisherTracks(offer.sdp);

                // Trace negotiation with tracks
                var tracksInfo = string.Join(";", tracks.Select(t => $"{t.TrackType}:{t.TrackId}"));
                _publisherTracer?.Trace(PeerConnectionTraceKey.NegotiateWithTracks, tracksInfo);

                //StreamTodo: mangle SDP
                var request = new SetPublisherRequest
                {
                    Sdp = offer.sdp,
                    SessionId = SessionId.ToString(),
                };
                request.Tracks.AddRange(tracks);

#if STREAM_DEBUG_ENABLED
                var serializedRequest = _serializer.Serialize(request);
                _logs.Warning($"SetPublisherRequest:\n{serializedRequest}");
#endif

                //StreamTODO: add cancellation token support
                var result = await RpcCallAsync(request, GeneratedAPI.SetPublisher, nameof(GeneratedAPI.SetPublisher),
                    GetCurrentCancellationTokenOrDefault(), response => response.Error);
                Publisher.ThrowDisposedDuringOperationIfNull();

#if STREAM_DEBUG_ENABLED
                _logs.Warning($"[Publisher] RemoteDesc (SDP Answer):\n{result.Sdp}");
#endif

                try
                {
                    await Publisher.SetRemoteDescriptionAsync(new RTCSessionDescription()
                    {
                        type = RTCSdpType.Answer,
                        sdp = result.Sdp
                    }, GetCurrentCancellationTokenOrDefault());

                    Publisher.AddPendingIceCandidates();
                }
                catch (Exception e)
                {
                    _publisherTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSetRemoteDescription,
                        e.Message ?? "unknown");
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown, don't log as error
            }
            catch (DisposedDuringOperationException)
            {
                // Expected during shutdown
            }
            catch (Exception e)
            {
                _publisherTracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSubmit, e.Message ?? "unknown");
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
            if (Publisher == null)
            {
                throw new ArgumentNullException($"{nameof(Publisher)} is null in {nameof(GetPublisherTracks)}");
            }

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
                    var videoLayers = GetPublisherVideoLayers(Publisher.VideoSender.GetParameters().encodings);
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

        //StreamTODO: delete
        private IEnumerable<VideoLayer> GetPublisherVideoLayers(IEnumerable<RTCRtpEncodingParameters> encodings)
        {
#if STREAM_DEBUG_ENABLED
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("GetPublisherVideoLayers:");
#endif
            foreach (var encoding in encodings)
            {
                var scaleBy = encoding.scaleResolutionDownBy ?? 1.0;
                var resolution = Publisher.GetLatestVideoSettings().MaxResolution;
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
            if (Subscriber != null)
            {
                DisposeSubscriber();
            }

            Subscriber = new SubscriberPeerConnection(_logs, iceServers, _subscriberTracer, _serializer,
                sfuClient: this);
            Subscriber.IceTrickled += OnIceTrickled;
            Subscriber.StreamAdded += OnSubscriberStreamAdded;
            Subscriber.ReconnectionNeeded += OnReconnectionNeeded;
        }

        private void DisposeSubscriber()
        {
            if (Subscriber != null)
            {
                Subscriber.IceTrickled -= OnIceTrickled;
                Subscriber.StreamAdded -= OnSubscriberStreamAdded;
                Subscriber.ReconnectionNeeded -= OnReconnectionNeeded;
                Subscriber.Dispose();
                Subscriber = null;
            }
        }

        /// <summary>
        /// Creating publisher requires active <see cref="IStreamCall"/>
        /// </summary>
        private void CreatePublisher(IEnumerable<ICEServer> iceServers)
        {
            //StreamTodo: Handle default settings -> speaker off, mic off, cam off. From call.Settings

            //StreamTODO: solve this differently. We probably need to keep old WS client live when migrating
            //But we don't want to create a leak
            if (Publisher != null)
            {
                DisposePublisher();
            }

            //StreamTODO: pass factory for WS creation. We may need two WS clients for migration so we can't rely on the same one
            Publisher = new PublisherPeerConnection(_logs, iceServers, this, _config.Audio, _publisherVideoSettings,
                sfuClient: this, _publisherTracer, _serializer);
            Publisher.IceTrickled += OnIceTrickled;
            Publisher.NegotiationNeeded += OnPublisherNegotiationNeeded;
            Publisher.PublisherAudioTrackChanged += OnPublisherAudioTrackChanged;
            Publisher.PublisherVideoTrackChanged += OnPublisherVideoTrackChanged;
            Publisher.ReconnectionNeeded += OnReconnectionNeeded;
            Publisher.Disconnected += PublisherOnDisconnected;
        }

        private void DisposePublisher()
        {
            if (Publisher != null)
            {
                Publisher.IceTrickled -= OnIceTrickled;
                Publisher.NegotiationNeeded -= OnPublisherNegotiationNeeded;
                Publisher.PublisherAudioTrackChanged -= OnPublisherAudioTrackChanged;
                Publisher.PublisherVideoTrackChanged -= OnPublisherVideoTrackChanged;
                Publisher.ReconnectionNeeded -= OnReconnectionNeeded;
                Publisher.Disconnected -= PublisherOnDisconnected;
                Publisher.Dispose();
                Publisher = null;
            }
        }

        /// <summary>
        /// Creates a new SfuWebSocket instance using the factory.
        /// Disposes the previous instance if one exists.
        /// </summary>
        /// <param name="previousSfuWebSocket">Returns the previous SfuWebSocket instance (if any) for cleanup after transition.</param>
        /// <returns>The newly created SfuWebSocket instance.</returns>
        private SfuWebSocket CreateNewSfuWebSocket(out SfuWebSocket previousSfuWebSocket)
        {
            previousSfuWebSocket = _sfuWebSocket;
            previousSfuWebSocket?.DebugMarkAsOld();

            if (_sfuWebSocket != null)
            {
                UnsubscribeFromSfuEvents(_sfuWebSocket);
            }

            _sfuWebSocket = _sfuWebSocketFactory.Create();

            SubscribeToSfuEvents(_sfuWebSocket);

#if STREAM_DEBUG_ENABLED
            _logs.Info($"[RtcSession] Created new SfuWebSocket instance");
#endif

            return _sfuWebSocket;
        }

        /// <summary>
        /// Disposes the current SfuWebSocket instance if one exists.
        /// </summary>
        private void DisposeSfuWebSocket()
        {
            if (_sfuWebSocket != null)
            {
                UnsubscribeFromSfuEvents(_sfuWebSocket);
                _sfuWebSocket.Dispose();
                _sfuWebSocket = null;
            }
        }

        private async Task ClosePreviousSfuWebSocketAsync(SfuWebSocket previousSfuWebSocket, string reason)
        {
            if (previousSfuWebSocket == null)
            {
                return;
            }

            try
            {
#if STREAM_DEBUG_ENABLED
                _logs.Info($"[RtcSession] Closing previous SfuWebSocket: reason={reason}");
#endif
                UnsubscribeFromSfuEvents(previousSfuWebSocket);
                await previousSfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure, reason);
#if STREAM_DEBUG_ENABLED
                _logs.Info($"[RtcSession] CLOSED previous SfuWebSocket: reason={reason}");
#endif
            }
            catch (Exception e)
            {
                _logs.Warning($"Failed to close previous SfuWebSocket: {e.Message}");
            }
            finally
            {
                previousSfuWebSocket.Dispose();
            }
        }

        private void OnPublisherAudioTrackChanged(AudioStreamTrack audioTrack)
        {
            UpdateAudioRecording();
            PublisherAudioTrackChanged?.Invoke();
        }

        private void OnPublisherVideoTrackChanged(VideoStreamTrack videoTrack)
        {
            PublisherVideoTrackChanged?.Invoke();
        }

        void PublisherOnDisconnected()
        {
            if (CallState == CallingState.Joined || CallState == CallingState.Joining)
            {
                PeerConnectionDisconnectedDuringSession?.Invoke();
            }
        }

        private void OnSfuWebSocketDisconnected()
        {
            AssertMainThread();
            // JS client doesn't trigger reconnect() in these cases
            switch (CallState)
            {
                // SFU WS closed before we finished joining
                case CallingState.Joining:

                // SFU WS closed due to unsuccessful join
                case CallingState.Idle:
                case CallingState.Left:

                // We're already reconnecting
                case CallingState.Reconnecting:
                    
#if STREAM_DEBUG_ENABLED
                    _logs.Info(
                        $"[RtcSession] OnSfuWebSocketDisconnected - ignored in state: " + CallState);
#endif
                    return;
            }

            if (_sfuWebSocket != null)
            {
                if (_sfuWebSocket.IsLeaving || _sfuWebSocket.IsClosingClean)
                {
#if STREAM_DEBUG_ENABLED
                    _logs.Info(
                        $"[RtcSession] OnSfuWebSocketDisconnected Ignored (IsLeaving={_sfuWebSocket.IsLeaving}, IsClosingClean={_sfuWebSocket.IsClosingClean})");
#endif
                    return;
                }

#if STREAM_DEBUG_ENABLED
                _logs.Info(
                    $"[RtcSession] OnSfuWebSocketDisconnected (IsLeaving={_sfuWebSocket.IsLeaving}, IsClosingClean={_sfuWebSocket.IsClosingClean})");
#endif
                SfuDisconnected?.Invoke();
            }

            var arePeerConnectionsHealthy = (Publisher?.IsHealthy ?? false) && (Subscriber?.IsHealthy ?? false);
            var strategy = arePeerConnectionsHealthy
                ? WebsocketReconnectStrategy.Fast
                : WebsocketReconnectStrategy.Rejoin;

            _logs.WarningIfDebug($"[Reconnect] SFU WS disconnected - triggering reconnect with strategy: {strategy}");
            Reconnect(strategy, "SFU WS was disconnected").LogIfFailed();
        }

        private async void OnNetworkAvailabilityChanged(bool isNetworkAvailable)
        {
            //StreamTODO: test if we're properly handling this triggered again while previous callback is still processed
            if (isNetworkAvailable)
            {
                try
                {
                    _logs.WarningIfDebug("Going Online");

                    if (CallState == CallingState.Joining || CallState == CallingState.Reconnecting ||
                        CallState == CallingState.Migrating || _isReconnecting)
                    {
                        _logs.WarningIfDebug(
                            $"{nameof(OnNetworkAvailabilityChanged)} skipped - reconnection already in progress. CallState: {CallState}");
                        return;
                    }

                    // Close the previous SFU WS client to force a clean WS join
                    // JS always re-creates the SFU WS when getting back online
                    if (_sfuWebSocket != null)
                    {
                        await _sfuWebSocket.DisconnectAsync(WebSocketCloseStatus.NormalClosure,
                            "Closing WS to reconnect after going online");
                    }

                    if (ActiveCall == null)
                    {
                        return;
                    }

                    var offlineTime = DateTime.UtcNow - _lastTimeOffline;
                    var strategy = offlineTime.TotalSeconds > _fastReconnectDeadlineSeconds
                        ? WebsocketReconnectStrategy.Rejoin
                        : WebsocketReconnectStrategy.Fast;

                    _logs.WarningIfDebug($"Reconnect triggered by {nameof(OnNetworkAvailabilityChanged)}. Strategy: {strategy}, offline time: {offlineTime}");
                    await Reconnect(strategy, "Going online");
                }
                catch (Exception e)
                {
                    _logs.Exception(e);
                }
            }
            else
            {
                _logs.WarningIfDebug("Going Offline");
                _lastTimeOffline = DateTime.UtcNow;
            }
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

        private void SubscribeToSfuEvents(SfuWebSocket sfuWebSocket)
        {
            sfuWebSocket.SubscriberOffer += OnSfuSubscriberOffer;
            sfuWebSocket.PublisherAnswer += OnSfuPublisherAnswer;
            sfuWebSocket.ConnectionQualityChanged += OnSfuConnectionQualityChanged;
            sfuWebSocket.AudioLevelChanged += OnSfuAudioLevelChanged;
            sfuWebSocket.IceTrickle += OnSfuIceTrickle;
            sfuWebSocket.ChangePublishQuality += OnSfuChangePublishQuality;
            sfuWebSocket.ParticipantJoined += OnSfuParticipantJoined;
            sfuWebSocket.ParticipantLeft += OnSfuParticipantLeft;
            sfuWebSocket.DominantSpeakerChanged += OnSfuDominantSpeakerChanged;
            sfuWebSocket.JoinResponse += OnSfuJoinResponse;
            sfuWebSocket.HealthCheck += OnSfuHealthCheck;
            sfuWebSocket.TrackPublished += OnSfuTrackPublished;
            sfuWebSocket.TrackUnpublished += OnSfuTrackUnpublished;
            sfuWebSocket.Error += OnSfuWebSocketOnError;
            sfuWebSocket.CallGrantsUpdated += OnSfuCallGrantsUpdated;
            sfuWebSocket.GoAway += OnSfuGoAway;
            sfuWebSocket.IceRestart += OnSfuIceRestart;
            sfuWebSocket.PinsUpdated += OnSfuPinsUpdated;
            sfuWebSocket.CallEnded += OnSfuWebSocketOnCallEnded;
            sfuWebSocket.ParticipantUpdated += OnSfuWebSocketOnParticipantUpdated;
            sfuWebSocket.ParticipantMigrationComplete += OnSfuWebSocketOnParticipantMigrationComplete;
            sfuWebSocket.ChangePublishOptions += OnSfuWebSocketOnChangePublishOptions;
            sfuWebSocket.InboundStateNotification += OnSfuInboundStateNotification;

            sfuWebSocket.Disconnected += OnSfuWebSocketDisconnected;
        }

        private void UnsubscribeFromSfuEvents(SfuWebSocket sfuWebSocket)
        {
            sfuWebSocket.SubscriberOffer -= OnSfuSubscriberOffer;
            sfuWebSocket.PublisherAnswer -= OnSfuPublisherAnswer;
            sfuWebSocket.ConnectionQualityChanged -= OnSfuConnectionQualityChanged;
            sfuWebSocket.AudioLevelChanged -= OnSfuAudioLevelChanged;
            sfuWebSocket.IceTrickle -= OnSfuIceTrickle;
            sfuWebSocket.ChangePublishQuality -= OnSfuChangePublishQuality;
            sfuWebSocket.ParticipantJoined -= OnSfuParticipantJoined;
            sfuWebSocket.ParticipantLeft -= OnSfuParticipantLeft;
            sfuWebSocket.DominantSpeakerChanged -= OnSfuDominantSpeakerChanged;
            sfuWebSocket.JoinResponse -= OnSfuJoinResponse;
            sfuWebSocket.HealthCheck -= OnSfuHealthCheck;
            sfuWebSocket.TrackPublished -= OnSfuTrackPublished;
            sfuWebSocket.TrackUnpublished -= OnSfuTrackUnpublished;
            sfuWebSocket.Error -= OnSfuWebSocketOnError;
            sfuWebSocket.CallGrantsUpdated -= OnSfuCallGrantsUpdated;
            sfuWebSocket.GoAway -= OnSfuGoAway;
            sfuWebSocket.IceRestart -= OnSfuIceRestart;
            sfuWebSocket.PinsUpdated -= OnSfuPinsUpdated;
            sfuWebSocket.CallEnded -= OnSfuWebSocketOnCallEnded;
            sfuWebSocket.ParticipantUpdated -= OnSfuWebSocketOnParticipantUpdated;
            sfuWebSocket.ParticipantMigrationComplete -= OnSfuWebSocketOnParticipantMigrationComplete;
            sfuWebSocket.ChangePublishOptions -= OnSfuWebSocketOnChangePublishOptions;
            sfuWebSocket.InboundStateNotification -= OnSfuInboundStateNotification;

            sfuWebSocket.Disconnected -= OnSfuWebSocketDisconnected;
        }

        private bool AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                _logs.Error("Called Not from the main thread!!!");
                //throw new InvalidOperationException("Called Not from the main thread!!!");
                
                //StreamTODO: fix this later, if not main thread then set a flag and execute by Update()

                return false;
            }

            return true;
        }
    }
}