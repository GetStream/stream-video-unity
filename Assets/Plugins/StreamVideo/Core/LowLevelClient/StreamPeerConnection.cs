using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Models;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using Unity.WebRTC;
using UnityEngine;

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

        public RTCSignalingState SignalingState => _peerConnection.SignalingState;

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers,
            Func<TrackKind, string> streamIdFactory, IMediaInputProvider mediaInputProvider)
        {
            _mediaInputProvider = mediaInputProvider;
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
            var videoTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Video, streamIdFactory);
            var audioTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Audio, streamIdFactory);


            //_audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);

            // var capabilities = RTCRtpSender.GetCapabilities(TrackKind.Video);
            // _videoTransceiver.SetCodecPreferences(capabilities.codecs);

            if (_peerType == StreamPeerType.Publisher)
            {
                var streamId = streamIdFactory(TrackKind.Video);
                var mediaStream = new MediaStream(streamId);
                var videoTrack = CreatePublisherVideoTrack();
                mediaStream.AddTrack(videoTrack);

                //This is critical so that local SDP has the
                //a=sendonly\r\na=msid:653a6b6b-0934-467b-9b64-13c3de470e9f:Video:162238662 f83260d1-5554-4f4a-baea-b33e4a8d6dd2
                //record
                videoTransceiverInit.streams = new[] { mediaStream };

                _videoTransceiver = _peerConnection.AddTransceiver(videoTrack, videoTransceiverInit);
                _videoTransceiver.Sender.ReplaceTrack(videoTrack);


                #region ForceCodec

                var capabilities = RTCRtpSender.GetCapabilities(TrackKind.Video);
                foreach (var codec in capabilities.codecs)
                {
                    Debug.LogWarning(
                        $"Available video codec - {nameof(codec.mimeType)}:{codec.mimeType}, {nameof(codec.sdpFmtpLine)}:{codec.sdpFmtpLine}");
                }

                var vp8Codec = capabilities.codecs.Single(c
                    => c.mimeType.IndexOf("vp8", StringComparison.OrdinalIgnoreCase) != -1);
                var preferredCodecs = new[] { vp8Codec };
                _videoTransceiver.SetCodecPreferences(preferredCodecs);
                
                #endregion

                foreach (var encoding in _videoTransceiver.Sender.GetParameters().encodings)
                {
                    _logs.Warning(
                        $"[{_peerType}] Added Encoding to transceiver - rid: {encoding.rid}, maxBitrate: {encoding.maxBitrate}, scaleResolutionDownBy: {encoding.scaleResolutionDownBy}");
                }

                _logs.Warning($"[{_peerType}] Added Transceivers: " + _peerConnection.GetTransceivers().Count());
            }
        }

        public void RestartIce() => _peerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer)
        {
            _logs.Warning($"[{_peerType}] Set LocalDesc:\n" + offer.sdp);
            return _peerConnection.SetLocalDescriptionAsync(ref offer);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer)
        {
            await _peerConnection.SetRemoteDescriptionAsync(ref offer);

            _logs.Warning(
                $"[{_peerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");

            foreach (var iceCandidate in _pendingIceCandidates)
            {
                if(!_peerConnection.AddIceCandidate(iceCandidate))
                {
                    _logs.Error($"[{_peerType}] AddIceCandidate failed: {iceCandidate.Print()}");
                }
            }
        }

        public void AddIceCandidate(RTCIceCandidateInit iceCandidateInit)
        {
            _logs.Warning(
                $"[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
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

        public void Update()
        {
            if (_publisherVideoTrackTexture != null && _mediaInputProvider.VideoInput != null)
            {
                Graphics.Blit(_mediaInputProvider.VideoInput, _publisherVideoTrackTexture);
            }
        }

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
        private readonly IMediaInputProvider _mediaInputProvider;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        private VideoStreamTrack _videoStreamTrack;
        private RenderTexture _publisherVideoTrackTexture;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning($"[{_peerType}] OnIceConnectionChange to: " + state);
        }

        private void OnNegotiationNeeded()
        {
            //_logs.Warning($"$$$$$$$ [{_peerType}] OnNegotiationNeeded");

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            _logs.Warning($"[{_peerType}] OnConnectionStateChange to: {state}");
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            _logs.Warning($"[{_peerType}] OnTrack {trackEvent.Track.GetType()}");

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

        private static RTCRtpTransceiverInit BuildTransceiverInit(StreamPeerType type, TrackKind kind,
            Func<TrackKind, string> streamIdFactory)
        {
            if (type == StreamPeerType.Subscriber)
            {
                return new RTCRtpTransceiverInit
                {
                    direction = RTCRtpTransceiverDirection.RecvOnly,
                };
            }

            var encodings = GetVideoEncodingParameters(kind).ToArray();

            return new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly,
                sendEncodings = encodings,
            };
        }

        private static IEnumerable<RTCRtpEncodingParameters> GetVideoEncodingParameters(TrackKind trackKind)
        {
            switch (trackKind)
            {
                case TrackKind.Audio:

                    var audioEncoding = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.MaxPublishAudioBitrate,
                        scaleResolutionDownBy = 1.0,
                        rid = "a"
                    };

                    yield return audioEncoding;

                    break;
                case TrackKind.Video:

                    var fullQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.FullPublishVideoBitrate,
                        scaleResolutionDownBy = 1.0,
                        rid = "f"
                    };

                    var halfQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.HalfPublishVideoBitrate,
                        scaleResolutionDownBy = 2.0,
                        rid = "h"
                    };

                    var quarterQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.QuarterPublishVideoBitrate,
                        scaleResolutionDownBy = 4.0,
                        rid = "q"
                    };

                    Debug.LogWarning($"Rid values: {fullQuality.rid}, {halfQuality.rid}, {quarterQuality.rid}");

                    yield return fullQuality;

                    //StreamTodo: re-add this later. Seems that simulcast doesn't work with H264 https://github.com/Unity-Technologies/com.unity.webrtc/issues/925
                    //yield return halfQuality;
                    //yield return quarterQuality;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackKind), trackKind, null);
            }
        }

        private VideoStreamTrack CreatePublisherVideoTrack()
        {
            var gfxType = SystemInfo.graphicsDeviceType;
            var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);

            //StreamTodo: hardcoded resolution
            _publisherVideoTrackTexture = new RenderTexture(1920, 1080, 0, format);

            return new VideoStreamTrack(_publisherVideoTrackTexture);
        }

        private AudioStreamTrack CreatePublisherAudioTrack()
        {
            return new AudioStreamTrack(_mediaInputProvider.AudioInput);
        }
    }
}