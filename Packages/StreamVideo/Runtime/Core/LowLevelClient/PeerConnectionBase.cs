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
        public event Action<RTCIceCandidate, StreamPeerType> IceTrickled; //StreamTODO: remove StreamPeerType

        public event Action Disconnected;

        public RTCSignalingState SignalingState => PeerConnection.SignalingState;

        protected PeerConnectionBase(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers,
            Tracer tracer)
        {
            Logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _peerType = peerType;
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

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
        }

        public void RestartIce() => PeerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer,
            CancellationToken cancellationToken)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] Set LocalDesc:\n" + offer.sdp);
#endif
            _tracer?.Trace(PeerConnectionTraceKey.SetLocalDescription, offer.sdp);
            return PeerConnection.SetLocalDescriptionAsync(ref offer, cancellationToken);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer, CancellationToken cancellationToken)
        {
            _tracer?.Trace(PeerConnectionTraceKey.SetRemoteDescription, offer.sdp);

            await PeerConnection.SetRemoteDescriptionAsync(ref offer, cancellationToken);

#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{_peerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");
#endif

            foreach (var iceCandidate in _pendingIceCandidates)
            {
                if (!PeerConnection.AddIceCandidate(iceCandidate))
                {
                    Logs.Error($"[{_peerType}] AddIceCandidate failed: {iceCandidate.Print()}");
                }
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
#endif
            _tracer?.Trace(PeerConnectionTraceKey.AddIceCandidate, iceCandidateInit.candidate);

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
            _tracer?.Trace(PeerConnectionTraceKey.CreateOffer, offer.sdp);
            return offer;
        }

        public async Task<RTCSessionDescription> CreateAnswerAsync(CancellationToken cancellationToken)
        {
            var answer = await PeerConnection.CreateAnswerAsync(cancellationToken);
            _tracer?.Trace(PeerConnectionTraceKey.CreateAnswer, answer.sdp);
            return answer;
        }

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
            Logs.Warning($"Disposing PeerConnection [{_peerType}]");
#endif

            PeerConnection.OnIceCandidate -= OnIceCandidate;
            PeerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            PeerConnection.OnIceGatheringStateChange -= OnIceGatheringStateChange;
            PeerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            PeerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            PeerConnection.OnTrack -= OnTrack;

            _tracer?.Trace(PeerConnectionTraceKey.Close, null);
            PeerConnection.Close();
            PeerConnection.Dispose();

#if STREAM_DEBUG_ENABLED
            Logs.Warning($"Disposed PeerConnection [{_peerType}]");
#endif
        }

        protected RTCPeerConnection PeerConnection { get; }
        protected ILogs Logs { get; }
        
        protected virtual void OnDisposing()
        {
        }
        
        protected virtual void OnUpdate()
        {
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
        
        private readonly StreamPeerType _peerType;
        private readonly Tracer _tracer;
        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning(
                $"[{_peerType}] OnIceCandidate: {(candidate == null ? "null (gathering complete)" : candidate.ToString())}");
#endif

            if (candidate == null)
            {
                // Null candidate signals that ICE gathering is complete
                _tracer?.Trace(PeerConnectionTraceKey.OnIceCandidate, "null (ICE gathering complete)");
                return;
            }

            _tracer?.Trace(PeerConnectionTraceKey.OnIceCandidate, candidate.ToString());
            IceTrickled?.Invoke(candidate, _peerType);
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] OnIceConnectionChange to: " + state);
#endif
            _tracer?.Trace(PeerConnectionTraceKey.OnIceConnectionStateChange, state.ToString());
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] OnIceGatheringStateChange to: " + state);
#endif
            _tracer?.Trace(PeerConnectionTraceKey.OnIceGatheringStateChange, state.ToString());
        }

        private void OnNegotiationNeeded()
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] OnNegotiationNeeded");
#endif

            _tracer?.Trace(PeerConnectionTraceKey.OnNegotiationNeeded, null);

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] OnConnectionStateChange to: {state}");
#endif
            _tracer?.Trace(PeerConnectionTraceKey.OnConnectionStateChange, state.ToString());

            if (state == RTCPeerConnectionState.Disconnected)
            {
                Disconnected?.Invoke();
            }
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
#if STREAM_DEBUG_ENABLED
            Logs.Warning($"[{_peerType}] OnTrack {trackEvent.Track.GetType()}");
#endif

            var trackType = trackEvent.Track is AudioStreamTrack ? "audio" : "video";
            var trackId = trackEvent.Track.Id;
            var streamIds = trackEvent.Streams != null && trackEvent.Streams.Any()
                ? string.Join(",", trackEvent.Streams.Select(s => s.Id))
                : "";
            var isEnabled = trackEvent.Track.Enabled;

            if (!string.IsNullOrEmpty(streamIds))
            {
                _tracer?.Trace(PeerConnectionTraceKey.OnTrack, $"{trackType}:{trackId} {streamIds}");
            }
            else
            {
                _tracer?.Trace(PeerConnectionTraceKey.OnTrack, $"{trackType}:{trackId}");
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