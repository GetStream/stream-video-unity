//StreamTodo: duplicated declaration of STREAM_NATIVE_AUDIO (also in RtcSession.cs) easy to get out of sync.

#if UNITY_ANDROID && !UNITY_EDITOR
#define STREAM_NATIVE_AUDIO //Defined in multiple files
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Core.Trace;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using StreamVideo.v1.Sfu.Models;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Wrapper around WebRTC Peer Connection instance
    /// </summary>
    internal abstract class PeerConnectionBase : IDisposable
    {
        public event Action<MediaStream> StreamAdded;

        public event Action NegotiationNeeded;
        public event Action<RTCIceCandidate, StreamPeerType> IceTrickled; //StreamTODO: remove StreamPeerType?

        public event Action Disconnected;

        //StreamTODO: change to custom delegate
        public event Action<WebsocketReconnectStrategy, string, StreamPeerType> ReconnectionNeeded;

        public RTCSignalingState SignalingState => PeerConnection.SignalingState;

        protected PeerConnectionBase(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers,
            Tracer tracer, ISerializer serializer, ISfuClient sfuClient)
        {
            Logs = logs ?? throw new ArgumentNullException(nameof(logs));
            PeerType = peerType;
            Tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            SfuClient = sfuClient ?? throw new ArgumentNullException(nameof(sfuClient));

            var rtcIceServers = new List<RTCIceServer>();

            foreach (var ice in iceServers)
            {
                rtcIceServers.Add(new RTCIceServer
                {
                    credential = ice.Password,
                    credentialType = RTCIceCredentialType.Password,
                    urls = ice.Urls.ToArray(),
                    username = ice.Username
                });
            }

            var conf = new RTCConfiguration
            {
                iceServers = rtcIceServers.ToArray(),
                iceTransportPolicy = RTCIceTransportPolicy.All,
                bundlePolicy = null,
                iceCandidatePoolSize = null
            };

            PeerConnection = new RTCPeerConnection(ref conf);
            PeerConnection.OnIceCandidate += OnIceCandidate;
            PeerConnection.OnIceConnectionChange += OnIceConnectionChange;
            PeerConnection.OnIceGatheringStateChange += OnIceGatheringStateChange;
            PeerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            PeerConnection.OnConnectionStateChange += OnConnectionStateChange;
            PeerConnection.OnTrack += OnTrack;
            
            //StreamTODO: add tracer "create"
            
            //StreamTODO: review missing trace events
        }
        
        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer,
            CancellationToken cancellationToken)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] Set LocalDesc:\n" + offer.sdp);
#endif
            Tracer?.Trace(PeerConnectionTraceKey.SetLocalDescription, offer.sdp);
            return PeerConnection.SetLocalDescriptionAsync(ref offer, cancellationToken);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer, CancellationToken cancellationToken)
        {
            Tracer?.Trace(PeerConnectionTraceKey.SetRemoteDescription, offer.sdp);

            await PeerConnection.SetRemoteDescriptionAsync(ref offer, cancellationToken);

#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{PeerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");
#endif
        }

        public void AddPendingIceCandidates()
        {
            foreach (var iceCandidate in _pendingIceCandidates)
            {
                if (!PeerConnection.AddIceCandidate(iceCandidate))
                {
                    Logs.Error($"[{PeerType}] AddIceCandidate failed: {iceCandidate.Print()}");
                }
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{PeerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
#endif
            Tracer?.Trace(PeerConnectionTraceKey.AddIceCandidate, iceCandidateInit.candidate);

            var iceCandidate = new RTCIceCandidate(iceCandidateInit);
            if (!IsRemoteDescriptionAvailable)
            {
                _pendingIceCandidates.Add(iceCandidate);
                return;
            }

            PeerConnection.AddIceCandidate(iceCandidate);
        }

        public async Task<RTCSessionDescription> CreateOfferAsync(CancellationToken cancellationToken)
        {
            var offer = await PeerConnection.CreateOfferAsync(cancellationToken);
            Tracer?.Trace(PeerConnectionTraceKey.CreateOffer, offer.sdp);
            return offer;
        }

        public async Task<RTCSessionDescription> CreateAnswerAsync(CancellationToken cancellationToken)
        {
            var answer = await PeerConnection.CreateAnswerAsync(cancellationToken);
            Tracer?.Trace(PeerConnectionTraceKey.CreateAnswer, answer.sdp);
            return answer;
        }

        //StreamTODO: perhaps no need to make a native call and just return _videoTransceiver, _audioTransceiver
        public IEnumerable<RTCRtpTransceiver> GetTransceivers() => PeerConnection.GetTransceivers();
        
        public void Update()
        {
            OnUpdate();
        }

        public Task<RTCStatsReport> GetStatsReportAsync(CancellationToken cancellationToken)
            => PeerConnection.GetStatsAsync(cancellationToken);

        public void Dispose()
        {
            OnDisposing();

#if STREAM_DEBUG_ENABLED
            Logs.Warning($"Disposing PeerConnection [{PeerType}]");
#endif

            PeerConnection.OnIceCandidate -= OnIceCandidate;
            PeerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            PeerConnection.OnIceGatheringStateChange -= OnIceGatheringStateChange;
            PeerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            PeerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            PeerConnection.OnTrack -= OnTrack;

            Tracer?.Trace(PeerConnectionTraceKey.Close, null);
            PeerConnection.Close();
            PeerConnection.Dispose();

#if STREAM_DEBUG_ENABLED
            Logs.Warning($"Disposed PeerConnection [{PeerType}]");
#endif
        }

        protected RTCPeerConnection PeerConnection { get; }
        protected ILogs Logs { get; }
        protected StreamPeerType PeerType { get; }
        protected ISerializer Serializer { get; }
        protected Tracer Tracer { get; }
        protected ISfuClient SfuClient { get; }
        
        protected bool IsIceRestarting { get; set; }
        
        protected virtual void OnDisposing()
        {
        }
        
        protected virtual void OnUpdate()
        {
        }
        
        protected abstract Task RestartIce();

        // On JS this is intentionally not an async method
        protected async Task TryRestartIce()
        {
            const string errorReason = "restartICE() failed, initiating reconnect";
            try
            {
                await RestartIce();
            }
            catch (NegotiationException negotiationException)
            {
                var isSignalLostError = (negotiationException.SfuErrorCode == ErrorCode.ParticipantSignalLost);
                var strategy = isSignalLostError ? WebsocketReconnectStrategy.Fast : WebsocketReconnectStrategy.Rejoin;
                ReconnectionNeeded?.Invoke(strategy, errorReason, PeerType);
            }
            catch (Exception e)
            {
                ReconnectionNeeded?.Invoke(WebsocketReconnectStrategy.Rejoin, errorReason, PeerType);
            }
        }
        
        protected CancellationToken GetCurrentCancellationTokenOrDefault()
        {
            return default; //StreamTODO: implement, take the token from RtcSession
        }
        
        private bool IsRemoteDescriptionAvailable
        {
            get
            {
                try
                {
                    // Throws exception if not set
                    return !string.IsNullOrEmpty(PeerConnection.RemoteDescription.sdp);
                }
                catch
                {
                    return false;
                }
            }
        }

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        //StreamTODO: properly handle async
        //Perhaps LogIfFailed() should be in the very start of the call chain
        private void HandleConnectionStateUpdate(RTCIceConnectionState connectionState)
        {
            //TODO: ignore if calling state Offline or Reconnecting -> handleConnectionStateUpdate()

            if (IsIceRestarting)
            {
                return;
            }

            switch (connectionState)
            {
                case RTCIceConnectionState.Failed:
                    TryRestartIce().LogIfFailed();
                    break;
                case RTCIceConnectionState.Disconnected:
                    
                    // TODO: here JS client does a timeout, checks if still Disconnected or Failed and only then triggers the restart
                    // But it looks like this is because the browser does some restarting which we don't have
                    
                    TryRestartIce().LogIfFailed();
                    break;
                case RTCIceConnectionState.Connected:
                    
                    // TODO: here, we'd clear the timeout if the Disconnected state initiated the timeout
                    
                    break;
            }
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{PeerType}] OnIceCandidate: {(candidate == null ? "null (gathering complete)" : candidate.ToString())}");
#endif

            if (candidate == null)
            {
                // Null candidate signals that ICE gathering is complete
                Tracer?.Trace(PeerConnectionTraceKey.OnIceCandidate, "null (ICE gathering complete)");
                return;
            }

            Tracer?.Trace(PeerConnectionTraceKey.OnIceCandidate, candidate.ToString());
            IceTrickled?.Invoke(candidate, PeerType);
        }
        
        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] OnConnectionStateChange to: {state}");
#endif
            Tracer?.Trace(PeerConnectionTraceKey.OnConnectionStateChange, state.ToString());
            
            //StreamTODO: trace getstats:
            /*
             *    if (this.tracer && (state === 'connected' || state === 'failed')) {
      try {
        const stats = await this.stats.get();
        this.tracer.trace('getstats', stats.delta);
      } catch (err) {
        this.tracer.trace('getstatsOnFailure', (err as Error).toString());
      }
    }
             * 
             */

            //StreamTODO: probably remove this after reconnection flow is completed.
            // We don't want SDK integrator trying to reconnect on his own
            // Then again, if he wants to know the state change it should be possible
            // So perhaps a global stateChanged event
            if (state == RTCPeerConnectionState.Disconnected || state == RTCPeerConnectionState.Failed)
            {
                Disconnected?.Invoke();
            }
            
            // Failed state means we need a new PC. It's not possible to recover
            if (state == RTCPeerConnectionState.Failed)
            {
                ReconnectionNeeded?.Invoke(WebsocketReconnectStrategy.Rejoin, "Connection failed", PeerType);
                return;
            }
            
            HandleConnectionStateUpdate(ToIceState(state));

            return;

            //StreamTODO: get rid of this. HandleConnectionStateUpdate() should depend on abstract state
            RTCIceConnectionState ToIceState(RTCPeerConnectionState connectionState)
            {
                switch (connectionState)
                {
                    case RTCPeerConnectionState.New: return RTCIceConnectionState.New;
                    case RTCPeerConnectionState.Connecting: return RTCIceConnectionState.Checking; 
                    case RTCPeerConnectionState.Connected: return RTCIceConnectionState.Connected;
                    case RTCPeerConnectionState.Disconnected: return RTCIceConnectionState.Disconnected;
                    case RTCPeerConnectionState.Failed: return RTCIceConnectionState.Failed;
                    case RTCPeerConnectionState.Closed: return RTCIceConnectionState.Closed;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(connectionState), connectionState, null);
                }
            }
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] OnIceConnectionChange to: " + state);
#endif
            Tracer?.Trace(PeerConnectionTraceKey.OnIceConnectionStateChange, state.ToString());
            
            HandleConnectionStateUpdate(state);
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] OnIceGatheringStateChange to: " + state);
#endif
            Tracer?.Trace(PeerConnectionTraceKey.OnIceGatheringStateChange, state.ToString());
        }

        private void OnNegotiationNeeded()
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] OnNegotiationNeeded");
#endif

            Tracer?.Trace(PeerConnectionTraceKey.OnNegotiationNeeded, null);

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{PeerType}] OnTrack {trackEvent.Track.GetType()}");
#endif

            var trackType = trackEvent.Track is AudioStreamTrack ? "audio" : "video";
            var trackId = trackEvent.Track.Id;
            var streamIds = trackEvent.Streams != null && trackEvent.Streams.Any()
                ? string.Join(",", trackEvent.Streams.Select(s => s.Id))
                : "";
            var isEnabled = trackEvent.Track.Enabled;

            if (!string.IsNullOrEmpty(streamIds))
            {
                Tracer?.Trace(PeerConnectionTraceKey.OnTrack, $"{trackType}:{trackId} {streamIds}");
            }
            else
            {
                Tracer?.Trace(PeerConnectionTraceKey.OnTrack, $"{trackType}:{trackId}");
            }

            foreach (var stream in trackEvent.Streams)
            {
                StreamAdded?.Invoke(stream);

                //StreamTodo: taken from android sdk, check why this is needed
                foreach (var audioTrack in stream.GetAudioTracks())
                {
                    audioTrack.Enabled = true;
                }
            }
        }
    }
}