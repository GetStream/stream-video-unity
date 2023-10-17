using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Configs;
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
        public MediaStream PublisherVideoMediaStream { get; private set; }
        public MediaStream PublisherAudioMediaStream { get; private set; }
        public RTCRtpSender VideoSender { get; private set; }

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers,
            IMediaInputProvider mediaInputProvider, IStreamAudioConfig audioConfig)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _mediaInputProvider = mediaInputProvider ?? throw new ArgumentNullException(nameof(mediaInputProvider));
            _peerType = peerType;
            _audioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));
            
            _mediaInputProvider.AudioInputChanged += OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged += OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged += OnVideoInputChanged;

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

            _peerConnection = new RTCPeerConnection(ref conf);
            _peerConnection.OnIceCandidate += OnIceCandidate;
            _peerConnection.OnIceConnectionChange += OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange += OnIceGatheringStateChange;
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange += OnConnectionStateChange;
            _peerConnection.OnTrack += OnTrack;

            if (_peerType == StreamPeerType.Publisher)
            {
                if (mediaInputProvider.AudioInput != null)
                {
                    CreatePublisherAudioTransceiver();
                }

                // StreamTodo: VideoSceneInput is not handled
                if (mediaInputProvider.VideoInput != null)
                {
                    CreatePublisherVideoTransceiver();
                }
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
                if (!_peerConnection.AddIceCandidate(iceCandidate))
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
            _mediaInputProvider.AudioInputChanged -= OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged -= OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged -= OnVideoInputChanged;
            
            _peerConnection.OnIceCandidate -= OnIceCandidate;
            _peerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange -= OnIceGatheringStateChange;
            _peerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            _peerConnection.OnTrack -= OnTrack;

            _peerConnection.Close();
        }

        private const string VideoCodecKeyH264 = "h264";
        private const string VideoCodecKeyVP8 = "vp8";
        private const string AudioCodecKeyRed = "red";

        private readonly RTCPeerConnection _peerConnection;

        private readonly ILogs _logs;
        private readonly StreamPeerType _peerType;
        private readonly IMediaInputProvider _mediaInputProvider;
        private readonly IStreamAudioConfig _audioConfig;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();
        
        private RTCRtpTransceiver _videoTransceiver;
        private RTCRtpTransceiver _audioTransceiver;

        private VideoStreamTrack _videoStreamTrack;
        private RenderTexture _publisherVideoTrackTexture;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            _logs.Warning($"[{_peerType}] OnIceConnectionChange to: " + state);
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
            _logs.Warning($"[{_peerType}] OnIceGatheringStateChange to: " + state);
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
        
        private void OnAudioInputChanged(AudioSource audio)
        {
            if (_mediaInputProvider.AudioInput != null)
            {
                CreatePublisherAudioTransceiver();
            }
        }

        private void OnVideoSceneInputChanged(Camera camera)
        {
            //StreamTodo: Implement OnVideoSceneInputChanged
        }

        private void OnVideoInputChanged(WebCamTexture webCamTexture)
        {
            if (_mediaInputProvider.VideoInput != null)
            {
                CreatePublisherVideoTransceiver();
            }
        }

        private static RTCRtpTransceiverInit BuildTransceiverInit(StreamPeerType type, TrackKind kind)
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
        
        private void CreatePublisherAudioTransceiver()
        {
            var audioTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Audio);
            _audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);

            PublisherAudioMediaStream = new MediaStream();
            
            var audioTrack = CreatePublisherAudioTrack();

            PublisherAudioMediaStream.AddTrack(audioTrack);
            _peerConnection.AddTrack(audioTrack, PublisherAudioMediaStream);

            if (_audioConfig.EnableRed)
            {
                ForceCodec(_audioTransceiver, AudioCodecKeyRed, TrackKind.Audio);
            }
        }

        private void CreatePublisherVideoTransceiver()
        {
            var videoTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Video);
            _videoTransceiver = _peerConnection.AddTransceiver(TrackKind.Video, videoTransceiverInit);

            PublisherVideoMediaStream = new MediaStream();
            var videoTrack = CreatePublisherVideoTrack();

            PublisherVideoMediaStream.AddTrack(videoTrack);
            videoTransceiverInit.streams = new[] { PublisherVideoMediaStream };

            ForceCodec(_videoTransceiver, VideoCodecKeyH264, TrackKind.Video);

            VideoSender = _videoTransceiver.Sender;
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
                        //minBitrate = RtcSession.FullPublishVideoBitrate / 2,
                        maxFramerate = 30,
                        scaleResolutionDownBy = 1.0,
                        rid = "f"
                    };
                    var halfQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.HalfPublishVideoBitrate,
                        //minBitrate = RtcSession.HalfPublishVideoBitrate / 2,
                        maxFramerate = 20,
                        scaleResolutionDownBy = 2.0,
                        rid = "h"
                    };

                    var quarterQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.QuarterPublishVideoBitrate,
                        //minBitrate = RtcSession.QuarterPublishVideoBitrate / 2,
                        maxFramerate = 10,
                        scaleResolutionDownBy = 4.0,
                        rid = "q"
                    };

                    Debug.LogWarning($"Rid values: {fullQuality.rid}, {halfQuality.rid}, {quarterQuality.rid}");

                    //StreamTodo: temporarily disabled because simulcast is not working with current Unity's WebRTC lib
                    yield return quarterQuality;
                    yield return halfQuality;
                    yield return fullQuality;

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

            Texture texture = _mediaInputProvider.VideoInput;

            if (_mediaInputProvider.VideoInput == null)
            {
                Debug.LogError("Video Input is null");
                texture = _publisherVideoTrackTexture;
            }

            Debug.LogWarning(
                $"CreatePublisherVideoTrack, isPlaying: {_mediaInputProvider.VideoInput.isPlaying}, readable: {_mediaInputProvider.VideoInput.isReadable}");

            return new VideoStreamTrack(_mediaInputProvider.VideoInput);
        }

        private VideoStreamTrack CreatePublisherVideoTrackFromSceneCamera()
        {
            var gfxType = SystemInfo.graphicsDeviceType;
            var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);

            //StreamTodo: hardcoded resolution
            _publisherVideoTrackTexture = new RenderTexture(1920, 1080, 0, format);

            var track = _mediaInputProvider.VideoSceneInput.CaptureStreamTrack(1920, 1080);
            return track;
        }

        private AudioStreamTrack CreatePublisherAudioTrack()
        {
            return new AudioStreamTrack(_mediaInputProvider.AudioInput);
        }

        private void ForceCodec(RTCRtpTransceiver transceiver, string codecKey, TrackKind kind)
        {
            var capabilities = RTCRtpSender.GetCapabilities(kind);
            var forcedCodecs = capabilities.codecs.Where(c
                => c.mimeType.IndexOf(codecKey, StringComparison.OrdinalIgnoreCase) != -1);

#if STREAM_DEBUG_ENABLED

            foreach (var codec in capabilities.codecs)
            {
                _logs.Info($"Available codec of kind `{kind}`: {codec.mimeType}");
            }

#endif

            if (!forcedCodecs.Any())
            {
                var availableCodecs = string.Join(", ", capabilities.codecs.Select(c => c.mimeType));
                _logs.Error(
                    $"Tried to filter codecs by `{codecKey}` key and kind `{kind}` but no results were found. Available codecs: {availableCodecs}");
                return;
            }

            foreach (var c in forcedCodecs)
            {
                _logs.Info($"Forced Codec of kind `{kind}`: {c.mimeType}, ");
            }

            var error = transceiver.SetCodecPreferences(forcedCodecs.ToArray());
            if (error != RTCErrorType.None)
            {
                _logs.Error($"Failed to set codecs for kind kind `{kind}` due to error: {error}");
            }
        }
    }
}