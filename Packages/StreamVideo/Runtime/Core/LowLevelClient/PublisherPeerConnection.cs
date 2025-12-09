using System;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.Configs;
using StreamVideo.Core.Models;
using StreamVideo.Core.Trace;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using Unity.WebRTC;
using UnityEngine;

namespace StreamVideo.Core.LowLevelClient
{
    internal class PublisherPeerConnection : PeerConnectionBase
    {
        public event Action<VideoStreamTrack> PublisherVideoTrackChanged;
        public event Action<AudioStreamTrack> PublisherAudioTrackChanged;

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

        // Full: 704×576  -> half: 352×288 -> quarter: 176×144 <- We want the smallest resolution to be above 96x96
        public static VideoResolution MinimumSafeTargetResolution => new VideoResolution(704, 576);

        public PublisherPeerConnection(ILogs logs, IEnumerable<ICEServer> iceServers,
            IMediaInputProvider mediaInputProvider, IStreamAudioConfig audioConfig,
            PublisherVideoSettings publisherVideoSettings, Tracer tracer)
            : base(logs, StreamPeerType.Publisher, iceServers, tracer)
        {
            _mediaInputProvider = mediaInputProvider ?? throw new ArgumentNullException(nameof(mediaInputProvider));
            _audioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));
            _publisherVideoSettings = publisherVideoSettings ??
                                      throw new ArgumentNullException(nameof(publisherVideoSettings));

            //if (_peerType == StreamPeerType.Publisher)
            {
                _mediaInputProvider.AudioInputChanged += OnAudioInputChanged;
                _mediaInputProvider.VideoSceneInputChanged += OnVideoSceneInputChanged;
                _mediaInputProvider.VideoInputChanged += OnVideoInputChanged;

                _mediaInputProvider.PublisherAudioTrackIsEnabledChanged += OnPublisherAudioTrackIsEnabledChanged;
                _mediaInputProvider.PublisherVideoTrackIsEnabledChanged += OnPublisherVideoTrackIsEnabledChanged;
            }
        }
        
        public PublisherVideoSettings GetLatestVideoSettings()
        {
            if (_publisherVideoSettings == null)
            {
                throw new ArgumentNullException(
                    $"{nameof(_publisherVideoSettings)} is null in {nameof(GetLatestVideoSettings)}");
            }

            _publisherVideoSettings.MaxResolution = PublisherTargetResolution;
            _publisherVideoSettings.FrameRate = PublisherTargetFrameRate;
            return _publisherVideoSettings;
        }

        /// <summary>
        /// Init publisher track in a separate method so that RtcSession can subscribe to events before creating tracks
        /// </summary>
        public void InitPublisherTracks()
        {
            //if (_peerType == StreamPeerType.Publisher)
            {
                ReplacePublisherAudioTrack();
                ReplacePublisherVideoTrack();

                // StreamTodo: VideoSceneInput is not handled
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            //StreamTodo: investigate if this Blit is necessary
            // One reason was to easy control target resolution -> we don't accept every target resolution because small res can crash Android video encoder
            // We should check if WebCamTexture allows setting any resolution
            if (_publisherVideoTrackTexture != null && _mediaInputProvider.VideoInput != null)
            {
                Graphics.Blit(_mediaInputProvider.VideoInput, _publisherVideoTrackTexture);
            }
        }
        
        protected override void OnDisposing()
        {
            _mediaInputProvider.AudioInputChanged -= OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged -= OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged -= OnVideoInputChanged;

            _mediaInputProvider.PublisherAudioTrackIsEnabledChanged -= OnPublisherAudioTrackIsEnabledChanged;
            _mediaInputProvider.PublisherVideoTrackIsEnabledChanged -= OnPublisherVideoTrackIsEnabledChanged;

            if (_publisherVideoTrackTexture != null)
            {
                // Unity gives warning when releasing an active texture
                if (RenderTexture.active == _publisherVideoTrackTexture)
                {
                    RenderTexture.active = null;
                }

                _publisherVideoTrackTexture.Release();
                _publisherVideoTrackTexture = null;
            }

            PublisherAudioTrack?.Stop();
            PublisherVideoTrack?.Stop();
            PublisherAudioTrack = null;
            PublisherVideoTrack = null;

            base.OnDisposing();
        }

        private VideoResolution PublisherTargetResolution
        {
            get
            {
                if (_mediaInputProvider.VideoInput != null)
                {
                    var preferred = new VideoResolution(_mediaInputProvider.VideoInput.width,
                        _mediaInputProvider.VideoInput.height);

                    // Requesting too small resolution can cause crashes in the Android video encoder
                    // The target resolution is used to calculate 3 layers of video encoding (full, half, quarter)
                    // For very small values of quarter resolution (not sure exact but ~100x100) the encoder crashes 

                    if (preferred.Width < MinimumSafeTargetResolution.Width ||
                        preferred.Height < MinimumSafeTargetResolution.Height)
                    {
                        return MinimumSafeTargetResolution;
                    }
                }

                return _publisherVideoSettings.MaxResolution;
            }
        }

        private uint PublisherTargetFrameRate
        {
            get
            {
                if (_mediaInputProvider.VideoInput != null)
                {
                    return (uint)_mediaInputProvider.VideoInput.requestedFPS;
                }

                return 30;
            }
        }

        private const string VideoCodecKeyH264 = "h264";
        private const string VideoCodecKeyVP8 = "vp8";
        private const string AudioCodecKeyRed = "red";

        //StreamTOdo: Finish implementation. This currently is not exposed to the user + it doesn't take into account VideoInput resolution. We need publisher resolution to match with WebCamTexture size
        private readonly PublisherVideoSettings _publisherVideoSettings;

        private readonly IMediaInputProvider _mediaInputProvider;
        private readonly IStreamAudioConfig _audioConfig;

        private RenderTexture _publisherVideoTrackTexture;
        private VideoStreamTrack _publisherVideoTrack;
        private AudioStreamTrack _publisherAudioTrack;

        private RTCRtpTransceiver _videoTransceiver;
        private RTCRtpTransceiver _audioTransceiver;

        private static RTCRtpTransceiverInit BuildTransceiverInit(TrackKind kind,
            PublisherVideoSettings publisherVideoSettings)
        {
            var encodings = GetVideoEncodingParameters(kind, publisherVideoSettings).ToArray();

            return new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly,
                sendEncodings = encodings,
            };
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

                    //StreamTodo: construct fewer layer when the target resolution is small. Android video encoder crashes when requesting very small resolution
                    // We're currently forcing the smallest safe resolution that the user can request so that the quarter layer doesn't reach too small resolution
                    // But we should allow setting small resolution and just produce fewer layers in that case

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

        private void CreatePublisherAudioTransceiverAndTrack()
        {
            var audioTransceiverInit = BuildTransceiverInit(TrackKind.Audio, _publisherVideoSettings);
            _audioTransceiver = PeerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);

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
            PeerConnection.AddTrack(audioTrack, PublisherAudioMediaStream);

            PublisherAudioTrack = audioTrack;

            Logs.WarningIfDebug(
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
            PeerConnection.RemoveTrack(_audioTransceiver.Sender);

#if STREAM_NATIVE_AUDIO
            PublisherAudioTrack.StopLocalAudioCapture();
#endif
            PublisherAudioTrack = null;
        }

        private void CreatePublisherVideoTransceiver()
        {
            var videoTransceiverInit = BuildTransceiverInit(TrackKind.Video, _publisherVideoSettings);

            PublisherVideoMediaStream = new MediaStream();
            PublisherVideoTrack = CreatePublisherVideoTrack();

            PublisherVideoMediaStream.AddTrack(PublisherVideoTrack);

            // Order seems fragile here in order to get correct msid record in local offer with the PublisherVideoMediaStream
            videoTransceiverInit.streams = new[] { PublisherVideoMediaStream };

            _videoTransceiver = PeerConnection.AddTransceiver(PublisherVideoTrack, videoTransceiverInit);

            ForceCodec(_videoTransceiver, VideoCodecKeyH264, TrackKind.Video);

            VideoSender = _videoTransceiver.Sender;
        }

        private void ReplaceActiveVideoTrack(VideoStreamTrack videoTrack)
        {
            PublisherVideoMediaStream.AddTrack(videoTrack);

            _videoTransceiver.Sender.ReplaceTrack(videoTrack);

            PublisherVideoTrack = videoTrack;
            Logs.WarningIfDebug(
                $"Executed {nameof(ReplaceActiveVideoTrack)} for video track not null: {videoTrack != null}");
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

            var res = PublisherTargetResolution;
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
            Logs.WarningIfDebug("[Audio] Created new AudioStreamTrack, enabled: " + track.Enabled);
            return track;
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

            Logs.WarningIfDebug("[Audio] Replacing publisher audio track");

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

        private void ForceCodec(RTCRtpTransceiver transceiver, string codecKey, TrackKind kind)
        {
            var capabilities = RTCRtpSender.GetCapabilities(kind);
            var forcedCodecs = capabilities.codecs.Where(c
                => c.mimeType.IndexOf(codecKey, StringComparison.OrdinalIgnoreCase) != -1);

#if STREAM_DEBUG_ENABLED
            Logs.Info($"Available codec of kind `{kind}`: " +
                      string.Join(", ", capabilities.codecs.Select(c => c.mimeType)));
#endif

            if (!forcedCodecs.Any())
            {
                var availableCodecs = string.Join(", ", capabilities.codecs.Select(c => c.mimeType));
                Logs.Error(
                    $"Tried to filter codecs by `{codecKey}` key and kind `{kind}` but no results were found. Available codecs: {availableCodecs}");
                return;
            }

#if STREAM_DEBUG_ENABLED
            foreach (var c in forcedCodecs)
            {
                Logs.Info($"Forced Codec of kind `{kind}`: {c.mimeType}, ");
            }
#endif

            var error = transceiver.SetCodecPreferences(forcedCodecs.ToArray());
            if (error != RTCErrorType.None)
            {
                Logs.Error($"Failed to set codecs for kind `{kind}` due to error: {error}");
            }
        }
    }
}