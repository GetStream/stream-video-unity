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

            //StreamTodo: for Publisher we need to wait for SFU connected in order to buildTrackId with state.trackLookupPrefix
            var videoTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Video);
            var audioTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Audio);

            _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video, videoTransceiverInit);
            _audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);
        }

        private static RTCRtpTransceiverInit BuildTransceiverInit(StreamPeerType type, TrackKind kind)
        {
            if (type == StreamPeerType.Subscriber)
            {
                switch (kind)
                {
                    case TrackKind.Audio:
                    case TrackKind.Video:
                        return new RTCRtpTransceiverInit
                        {
                            direction = RTCRtpTransceiverDirection.RecvOnly,
                        };
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }

            if (type == StreamPeerType.Publisher)
            {
                switch (kind)
                {
                    case TrackKind.Audio:

                        var audioEncoding = new RTCRtpEncodingParameters
                        {
                            active = true,
                            maxBitrate = 500_000,
                            //minBitrate = null,
                            //maxFramerate = null,
                            scaleResolutionDownBy = 1.0,
                            rid = "a"
                        };

                        return new RTCRtpTransceiverInit
                        {
                            direction = RTCRtpTransceiverDirection.SendOnly,
                            sendEncodings = new RTCRtpEncodingParameters[]
                            {
                                audioEncoding
                            }
                        };

                    case TrackKind.Video:

                        //StreamTodo: move to some config + perhaps allow user to set this
                        var maxPublishingBitrate = (ulong)1_200_000;

                        var fullQuality = new RTCRtpEncodingParameters
                        {
                            active = true,
                            maxBitrate = maxPublishingBitrate,
                            scaleResolutionDownBy = 1.0,
                            rid = "f"
                        };

                        var halfQuality = new RTCRtpEncodingParameters
                        {
                            active = true,
                            maxBitrate = maxPublishingBitrate / 2,
                            scaleResolutionDownBy = 2.0,
                            rid = "f"
                        };

                        var quarterQuality = new RTCRtpEncodingParameters
                        {
                            active = true,
                            maxBitrate = maxPublishingBitrate / 4,
                            scaleResolutionDownBy = 4.0,
                            rid = "f"
                        };

                        return new RTCRtpTransceiverInit
                        {
                            direction = RTCRtpTransceiverDirection.SendOnly,
                            sendEncodings = new RTCRtpEncodingParameters[]
                            {
                                fullQuality, halfQuality, quarterQuality
                            }
                        };
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
            _logs.Warning(
                $"---------------------[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
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

        public IEnumerable<RTCRtpTransceiver> GetTransceivers() => _peerConnection.GetTransceivers();

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