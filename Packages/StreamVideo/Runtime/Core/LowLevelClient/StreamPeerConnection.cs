//StreamTodo: duplicated declaration of STREAM_NATIVE_AUDIO (also in RtcSession.cs) easy to get out of sync.

#if UNITY_ANDROID && !UNITY_EDITOR
#define STREAM_NATIVE_AUDIO //Defined in multiple files
#endif
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

        public event Action<VideoStreamTrack> PublisherVideoTrackChanged;
        public event Action<AudioStreamTrack> PublisherAudioTrackChanged;

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

        public AudioStreamTrack PublisherAudioTrack
        {
            get => _publisherAudioTrack;
            private set
            {
                if (_publisherAudioTrack == value)
                {
                    return;
                }

                _publisherAudioTrack = value;
                PublisherAudioTrackChanged?.Invoke(_publisherAudioTrack);
            }
        }

        public VideoStreamTrack PublisherVideoTrack
        {
            get => _publisherVideoTrack;
            private set
            {
                if (_publisherVideoTrack == value)
                {
                    return;
                }

                _publisherVideoTrack = value;
                PublisherVideoTrackChanged?.Invoke(_publisherVideoTrack);
            }
        }

        public RTCRtpSender VideoSender { get; private set; }

        public StreamPeerConnection(ILogs logs, StreamPeerType peerType, IEnumerable<ICEServer> iceServers,
            IMediaInputProvider mediaInputProvider, IStreamAudioConfig audioConfig,
            PublisherVideoSettings publisherVideoSettings)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _peerType = peerType;
            _mediaInputProvider = mediaInputProvider ?? throw new ArgumentNullException(nameof(mediaInputProvider));
            _audioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));
            _publisherVideoSettings = publisherVideoSettings ??
                                      throw new ArgumentNullException(nameof(publisherVideoSettings));

            if (_peerType == StreamPeerType.Publisher)
            {
                _mediaInputProvider.AudioInputChanged += OnAudioInputChanged;
                _mediaInputProvider.VideoSceneInputChanged += OnVideoSceneInputChanged;
                _mediaInputProvider.VideoInputChanged += OnVideoInputChanged;
                
                _mediaInputProvider.PublisherAudioTrackIsEnabledChanged += OnPublisherAudioTrackIsEnabledChanged;
                _mediaInputProvider.PublisherVideoTrackIsEnabledChanged += OnPublisherVideoTrackIsEnabledChanged;
            }

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
        }

        /// <summary>
        /// Init publisher track in a separate method so that RtcSession can subscribe to events before creating tracks
        /// </summary>
        public void InitPublisherTracks()
        {
            if (_peerType == StreamPeerType.Publisher)
            {
                ReplacePublisherAudioTrack();
                ReplacePublisherVideoTrack();
                
                // StreamTodo: VideoSceneInput is not handled
            }
        }

        public void RestartIce() => _peerConnection.RestartIce();

        public Task SetLocalDescriptionAsync(ref RTCSessionDescription offer)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] Set LocalDesc:\n" + offer.sdp);
#endif
            return _peerConnection.SetLocalDescriptionAsync(ref offer);
        }

        public async Task SetRemoteDescriptionAsync(RTCSessionDescription offer)
        {
            await _peerConnection.SetRemoteDescriptionAsync(ref offer);

#if STREAM_DEBUG_ENABLED
            _logs.Warning(
                $"[{_peerType}] Set RemoteDesc & send pending ICE Candidates: {_pendingIceCandidates.Count}, IsRemoteDescriptionAvailable: {IsRemoteDescriptionAvailable}, offer:\n{offer.sdp}");
#endif

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
#if STREAM_DEBUG_ENABLED
            _logs.Warning(
                $"[{_peerType}] Add ICE Candidate, remote available: {IsRemoteDescriptionAvailable}, candidate: {iceCandidateInit.candidate}");
#endif
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
            //StreamTodo: investigate if this Blit is necessary
            if (_publisherVideoTrackTexture != null && _mediaInputProvider.VideoInput != null)
            {
                Graphics.Blit(_mediaInputProvider.VideoInput, _publisherVideoTrackTexture);
            }
        }

        public Task<RTCStatsReport> GetStatsReportAsync() => _peerConnection.GetStatsAsync();

        public void Dispose()
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"Disposing PeerConnection [{_peerType}]");
#endif

#if STREAM_NATIVE_AUDIO
            if (PublisherAudioTrack != null)
            {
                //StreamTODO: call this when PublisherAudioTrack is set to null
                PublisherAudioTrack.StopLocalAudioCapture();
            }
#endif

            PublisherAudioTrack?.Stop();
            PublisherVideoTrack?.Stop();
            PublisherAudioTrack = null;
            PublisherVideoTrack = null;

            _mediaInputProvider.AudioInputChanged -= OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged -= OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged -= OnVideoInputChanged;
            
            _mediaInputProvider.PublisherAudioTrackIsEnabledChanged -= OnPublisherAudioTrackIsEnabledChanged;
            _mediaInputProvider.PublisherVideoTrackIsEnabledChanged -= OnPublisherVideoTrackIsEnabledChanged;

            _peerConnection.OnIceCandidate -= OnIceCandidate;
            _peerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange -= OnIceGatheringStateChange;
            _peerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            _peerConnection.OnTrack -= OnTrack;

            _peerConnection.Close();

#if STREAM_DEBUG_ENABLED
            _logs.Warning($"Disposed PeerConnection [{_peerType}]");
#endif
        }

        private const string VideoCodecKeyH264 = "h264";
        private const string VideoCodecKeyVP8 = "vp8";
        private const string AudioCodecKeyRed = "red";

        private readonly RTCPeerConnection _peerConnection;

        private readonly ILogs _logs;
        private readonly StreamPeerType _peerType;
        private readonly IMediaInputProvider _mediaInputProvider;
        private readonly IStreamAudioConfig _audioConfig;
        private readonly PublisherVideoSettings _publisherVideoSettings;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();

        private RTCRtpTransceiver _videoTransceiver;
        private RTCRtpTransceiver _audioTransceiver;

        private RenderTexture _publisherVideoTrackTexture;
        private VideoStreamTrack _publisherVideoTrack;
        private AudioStreamTrack _publisherAudioTrack;

        private void OnIceCandidate(RTCIceCandidate candidate) => IceTrickled?.Invoke(candidate, _peerType);

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] OnIceConnectionChange to: " + state);
#endif
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] OnIceGatheringStateChange to: " + state);
#endif
        }

        private void OnNegotiationNeeded()
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] OnNegotiationNeeded");
#endif

            //StreamTodo: take into account race conditions https://blog.mozilla.org/webrtc/perfect-negotiation-in-webrtc/
            //We want to set the local description if signalingState is stable - we need to check it because state could change during async operations

            NegotiationNeeded?.Invoke();
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] OnConnectionStateChange to: {state}");
#endif
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"[{_peerType}] OnTrack {trackEvent.Track.GetType()}");
#endif

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

        private void OnAudioInputChanged(AudioSource audio) => ReplacePublisherAudioTrack();

        private void OnPublisherAudioTrackIsEnabledChanged(bool isEnabled)
        {
            if (isEnabled && PublisherAudioTrack == null)
            {
                ReplacePublisherAudioTrack();
            }
        }

        /// <summary>
        /// Needed to init or when device changes
        /// </summary>
        private void ReplacePublisherAudioTrack()
        {
            var isActive = _mediaInputProvider.AudioInput != null && _mediaInputProvider.PublisherAudioTrackIsEnabled;
            if (!isActive)
            {
                TryClearPublisherAudioTrack();
                return;
            }
            
            _logs.WarningIfDebug("[Audio] Replacing publisher audio track");

            if (_audioTransceiver == null)
            {
                CreatePublisherAudioTransceiverAndTrack();
                return;
            }

            var newAudioTrack = CreatePublisherAudioTrack();

            TryClearPublisherAudioTrack();
            SetPublisherActiveAudioTrack(newAudioTrack);
        }

        private void OnVideoInputChanged(WebCamTexture webCamTexture) => ReplacePublisherVideoTrack();
        
        private void OnPublisherVideoTrackIsEnabledChanged(bool isEnabled)
        {
            if (isEnabled && _publisherVideoTrack == null)
            {
                ReplacePublisherVideoTrack();
            }
        }

        /// <summary>
        /// Needed to init or when device changes
        /// </summary>
        private void ReplacePublisherVideoTrack()
        {
            var isActive = _mediaInputProvider.VideoInput != null && _mediaInputProvider.PublisherVideoTrackIsEnabled;
            if (!isActive)
            {
                TryClearVideoTrack();
                return;
            }

            if (_videoTransceiver == null)
            {
                CreatePublisherVideoTransceiver();
                return;
            }

            var newVideoTrack = CreatePublisherVideoTrack();

            TryClearVideoTrack();
            ReplaceActiveVideoTrack(newVideoTrack);
        }

        private void OnVideoSceneInputChanged(Camera camera)
        {
            //StreamTodo: Implement OnVideoSceneInputChanged
        }

        private static RTCRtpTransceiverInit BuildTransceiverInit(StreamPeerType type, TrackKind kind,
            PublisherVideoSettings publisherVideoSettings)
        {
            if (type == StreamPeerType.Subscriber)
            {
                return new RTCRtpTransceiverInit
                {
                    direction = RTCRtpTransceiverDirection.RecvOnly,
                };
            }

            var encodings = GetVideoEncodingParameters(kind, publisherVideoSettings).ToArray();

            return new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly,
                sendEncodings = encodings,
            };
        }

        private void CreatePublisherAudioTransceiverAndTrack()
        {
            var audioTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Audio, _publisherVideoSettings);
            _audioTransceiver = _peerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);

            PublisherAudioMediaStream = new MediaStream();

            var audioTrack = CreatePublisherAudioTrack();
            SetPublisherActiveAudioTrack(audioTrack);

            if (_audioConfig.EnableRed)
            {
                ForceCodec(_audioTransceiver, AudioCodecKeyRed, TrackKind.Audio);
            }
        }

        private void SetPublisherActiveAudioTrack(AudioStreamTrack audioTrack)
        {
            PublisherAudioMediaStream.AddTrack(audioTrack);

            //StreamTodo: check if this line is needed
            _peerConnection.AddTrack(audioTrack, PublisherAudioMediaStream);

            PublisherAudioTrack = audioTrack;

            _logs.WarningIfDebug(
                $"Executed {nameof(SetPublisherActiveAudioTrack)} for audio track not null: {audioTrack != null}");
        }

        private void TryClearPublisherAudioTrack()
        {
            if (PublisherAudioTrack == null)
            {
                return;
            }

            PublisherAudioTrack.Stop();

            PublisherAudioMediaStream.RemoveTrack(PublisherAudioTrack);
            _peerConnection.RemoveTrack(_audioTransceiver.Sender);

#if STREAM_NATIVE_AUDIO
            PublisherAudioTrack.StopLocalAudioCapture();
#endif
            PublisherAudioTrack = null;
        }

        private void CreatePublisherVideoTransceiver()
        {
            var videoTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Video, _publisherVideoSettings);

            PublisherVideoMediaStream = new MediaStream();
            PublisherVideoTrack = CreatePublisherVideoTrack();

            PublisherVideoMediaStream.AddTrack(PublisherVideoTrack);

            // Order seems fragile here in order to get correct msid record in local offer with the PublisherVideoMediaStream
            videoTransceiverInit.streams = new[] { PublisherVideoMediaStream };

            _videoTransceiver = _peerConnection.AddTransceiver(PublisherVideoTrack, videoTransceiverInit);

            ForceCodec(_videoTransceiver, VideoCodecKeyH264, TrackKind.Video);

            VideoSender = _videoTransceiver.Sender;
        }

        private void ReplaceActiveVideoTrack(VideoStreamTrack videoTrack)
        {
            PublisherVideoMediaStream.AddTrack(videoTrack);

            _videoTransceiver.Sender.ReplaceTrack(videoTrack);

            PublisherVideoTrack = videoTrack;
            _logs.WarningIfDebug(
                $"Executed {nameof(ReplaceActiveVideoTrack)} for video track not null: {videoTrack != null}");
        }

        private void TryClearVideoTrack()
        {
            if (PublisherVideoTrack == null)
            {
                return;
            }

            PublisherVideoTrack.Stop();

            PublisherVideoMediaStream.RemoveTrack(PublisherVideoTrack);

            PublisherVideoTrack = null;
        }

        private static IEnumerable<RTCRtpEncodingParameters> GetVideoEncodingParameters(TrackKind trackKind,
            PublisherVideoSettings publisherVideoSettings)
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
                        maxFramerate = publisherVideoSettings.FrameRate,
                        scaleResolutionDownBy = 1.0,
                        rid = "f"
                    };
                    var halfQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.HalfPublishVideoBitrate,
                        //minBitrate = RtcSession.HalfPublishVideoBitrate / 2,
                        maxFramerate = publisherVideoSettings.FrameRate,
                        scaleResolutionDownBy = 2.0,
                        rid = "h"
                    };

                    var quarterQuality = new RTCRtpEncodingParameters
                    {
                        active = true,
                        maxBitrate = RtcSession.QuarterPublishVideoBitrate,
                        //minBitrate = RtcSession.QuarterPublishVideoBitrate / 2,
                        maxFramerate = publisherVideoSettings.FrameRate,
                        scaleResolutionDownBy = 4.0,
                        rid = "q"
                    };

#if STREAM_DEBUG_ENABLED
                    Debug.LogWarning($"Rid values: {fullQuality.rid}, {halfQuality.rid}, {quarterQuality.rid}");
#endif

                    yield return quarterQuality;
                    yield return halfQuality;
                    yield return fullQuality;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackKind), trackKind, null);
            }
        }

        private VideoResolution GetPublisherResolution()
        {
            if (_mediaInputProvider.VideoInput != null)
            {
                var maxResolution = _publisherVideoSettings.MaxResolution;
                return new VideoResolution(maxResolution.Width, maxResolution.Height);
            }

            return VideoResolution.Res_1080p;
        }

        private VideoStreamTrack CreatePublisherVideoTrack()
        {
            if (_mediaInputProvider.VideoInput == null)
            {
                throw new ArgumentException(
                    $"Can't create publisher video track because `{nameof(_mediaInputProvider.VideoInput)}` is not null");
            }

            var gfxType = SystemInfo.graphicsDeviceType;
            var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);

            var res = GetPublisherResolution();
            _publisherVideoTrackTexture = new RenderTexture((int)res.Width, (int)res.Height, 0, format);

#if STREAM_DEBUG_ENABLED
            Debug.LogWarning(
                $"CreatePublisherVideoTrack, isPlaying: {_mediaInputProvider.VideoInput.isPlaying}, readable: {_mediaInputProvider.VideoInput.isReadable}");
#endif

            var track = new VideoStreamTrack(_publisherVideoTrackTexture);
            track.Enabled = _mediaInputProvider.PublisherVideoTrackIsEnabled;
            return track;
        }

        //StreamTodo: CreatePublisherVideoTrackFromSceneCamera() is not used in any path
        private VideoStreamTrack CreatePublisherVideoTrackFromSceneCamera()
        {
            var gfxType = SystemInfo.graphicsDeviceType;
            var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);

            //StreamTodo: hardcoded resolution
            _publisherVideoTrackTexture = new RenderTexture(1920, 1080, 0, format);

            var track = _mediaInputProvider.VideoSceneInput.CaptureStreamTrack(1920, 1080);
            track.Enabled = _mediaInputProvider.PublisherVideoTrackIsEnabled;
            return track;
        }

        private AudioStreamTrack CreatePublisherAudioTrack()
        {
#if STREAM_NATIVE_AUDIO
            // Removed passing AudioSource so that AudioFilter is not created and ProcessLocalAudio is not called inside webrtc plugin
            var track = new AudioStreamTrack();
#else
            var track = new AudioStreamTrack(_mediaInputProvider.AudioInput);
#endif

            track.Enabled = _mediaInputProvider.PublisherAudioTrackIsEnabled;
            _logs.WarningIfDebug("[Audio] Created new AudioStreamTrack, enabled: " + track.Enabled);
            return track;
        }

        private void ForceCodec(RTCRtpTransceiver transceiver, string codecKey, TrackKind kind)
        {
            var capabilities = RTCRtpSender.GetCapabilities(kind);
            var forcedCodecs = capabilities.codecs.Where(c
                => c.mimeType.IndexOf(codecKey, StringComparison.OrdinalIgnoreCase) != -1);

#if STREAM_DEBUG_ENABLED
            _logs.Info($"Available codec of kind `{kind}`: " +
                       string.Join(", ", capabilities.codecs.Select(c => c.mimeType)));
#endif

            if (!forcedCodecs.Any())
            {
                var availableCodecs = string.Join(", ", capabilities.codecs.Select(c => c.mimeType));
                _logs.Error(
                    $"Tried to filter codecs by `{codecKey}` key and kind `{kind}` but no results were found. Available codecs: {availableCodecs}");
                return;
            }

#if STREAM_DEBUG_ENABLED
            foreach (var c in forcedCodecs)
            {
                _logs.Info($"Forced Codec of kind `{kind}`: {c.mimeType}, ");
            }
#endif

            var error = transceiver.SetCodecPreferences(forcedCodecs.ToArray());
            if (error != RTCErrorType.None)
            {
                _logs.Error($"Failed to set codecs for kind `{kind}` due to error: {error}");
            }
        }
    }
}