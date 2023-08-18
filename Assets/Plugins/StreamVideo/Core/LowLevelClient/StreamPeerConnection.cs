using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Libs.Logs;
using Unity.WebRTC;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Wrapper around WebRTC Peer Connection instance
    /// </summary>
    internal class StreamPeerConnection : IDisposable
    {
        public event Action<MediaStream> StreamAdded;

        public event Action NegotiationNeeded;
        public event Action<RTCIceCandidate, StreamPeerType> IceTrickled;

        public bool IsRemoteDescriptionAvailable
        {
            get
            {
                try
                {
                    // Throws exception if not set
                    return !string.IsNullOrEmpty(_peerConnection.RemoteDescription.sdp);
                }
                catch
                {
                    return false;
                }
            }
        }

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers)
        {
            _peerType = peerType;
            _logs = logs;

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
                iceTransportPolicy = RTCIceTransportPolicy.Relay,
                bundlePolicy = null,
                iceCandidatePoolSize = null
            };

            _peerConnection = new RTCPeerConnection(ref conf);
            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnIceConnectionChange += OnIceConnectionChange;
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChange;
            _peerConnection.OnTrack += OnTrack;

            var direction = _peerType == StreamPeerType.Publisher
                ? RTCRtpTransceiverDirection.SendOnly
                : RTCRtpTransceiverDirection.RecvOnly;
            //StreamTodo: review adding transceivers (probably needed to disable/enable tracks during session)
            // _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video, new RTCRtpTransceiverInit
            // {
            //     direction = direction,
            //     // sendEncodings = new RTCRtpEncodingParameters[]
            //     // {
            //     // },
            //     streams = new MediaStream[]
            //     {
            //         ReceiveStream
            //     }
            // });
            //_audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio);
        }

        public void RestartIce() => _peerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer)
        {
            _logs.Warning($"------------------- [{_peerType}] Set LocalDesc:\n" + offer.sdp);
            return _peerConnection.SetLocalDescriptionAsync(ref offer);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer)
        {
            await _peerConnection.SetRemoteDescriptionAsync(ref offer);

            _logs.Warning(
                $"------------------- [{_peerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");

            foreach (var iceCandidate in _pendingIceCandidates)
            {
                _peerConnection.AddIceCandidate(iceCandidate);
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
            _logs.Warning($"---------------------[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
            var iceCandidate = new RTCIceCandidate(iceCandidateInit);
            if (!IsRemoteDescriptionAvailable)
            {
                _pendingIceCandidates.Add(iceCandidate);
                return;
            }

            _peerConnection.AddIceCandidate(iceCandidate);
        }

        public Task<RTCSessionDescription> CreateOfferAsync() => _peerConnection.CreateOfferAsync();
        
        public Task<RTCSessionDescription> CreateAnswerAsync() => _peerConnection.CreateAnswerAsync();

        public void Dispose()
        {
            _peerConnection.OnIceCandidate -= OnIceCandidate;
            _peerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            _peerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            _peerConnection.OnTrack -= OnTrack;

            _peerConnection.Close();
        }

        private readonly RTCPeerConnection _peerConnection;
        private readonly RTCRtpTransceiver _videoTransceiver;
        private readonly RTCRtpTransceiver _audioTransceiver;
        private readonly ILogs _logs;
        private readonly StreamPeerType _peerType;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        private VideoStreamTrack _videoStreamTrack;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnIceConnectionChange to: " + state);
        }

        private void OnNegotiationNeeded()
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnNegotiationNeeded");

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnConnectionStateChange to: {state}");
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            _logs.Warning($"$$$$$$$ [{_peerType}] OnTrack {trackEvent.Track.GetType()}");

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