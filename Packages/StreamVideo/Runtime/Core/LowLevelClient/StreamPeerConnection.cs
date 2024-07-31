using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Configs;
using StreamVideo.Core.Models;
using StreamVideo.Core.StatefulModels;
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
                _mediaInputProvider.CustomVideoSourceAdded += OnCustomVideoSourceAdded;
                _mediaInputProvider.CustomVideoSourceRemoved += OnCustomVideoSourceRemoved;
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

        private void OnCustomVideoSourceAdded((CustomTrackHandle handle, RenderTexture source, uint frameRate) obj)
            => CreatePublisherCustomVideoTransceiver(obj.source, obj.frameRate);

        private void OnCustomVideoSourceRemoved(CustomTrackHandle trackHandle)
        {
            // StreamTodo: handle removing video source
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
        
        public bool TryGetMediaStreamFor(RTCRtpTransceiver transceiver, out MediaStream mediaStream)
            => _transceiverToMediaStream.TryGetValue(transceiver, out mediaStream);

        public void Dispose()
        {
            _mediaInputProvider.AudioInputChanged -= OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged -= OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged -= OnVideoInputChanged;
            _mediaInputProvider.CustomVideoSourceAdded -= OnCustomVideoSourceAdded;
            _mediaInputProvider.CustomVideoSourceRemoved -= OnCustomVideoSourceRemoved;

            _peerConnection.OnIceCandidate -= OnIceCandidate;
            _peerConnection.OnIceConnectionChange -= OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange -= OnIceGatheringStateChange;
            _peerConnection.OnNegotiationNeeded -= OnNegotiationNeeded;
            _peerConnection.OnConnectionStateChange -= OnConnectionStateChange;
            _peerConnection.OnTrack -= OnTrack;

            PublisherAudioTrack?.Stop();
            PublisherVideoTrack?.Stop();
            
            foreach (var mediaStream in _transceiverToMediaStream.Values)
            {
                foreach(var track in mediaStream.GetTracks())
                {
                    track.Stop();
                }
            }
            
            _transceiverToMediaStream.Clear();

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
        private readonly PublisherVideoSettings _publisherVideoSettings;

        private readonly List<RTCIceCandidate> _pendingIceCandidates = new List<RTCIceCandidate>();
        
        private readonly Dictionary<RTCRtpTransceiver, MediaStream> _transceiverToMediaStream
            = new Dictionary<RTCRtpTransceiver, MediaStream>();

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

        private void OnAudioInputChanged(AudioSource audio)
        {
            if (_mediaInputProvider.AudioInput == null)
            {
                TryClearPublisherAudioTrack();
                return;
            }

            if (_audioTransceiver == null)
            {
                CreatePublisherAudioTransceiver();
                return;
            }

            var newAudioTrack = CreatePublisherAudioTrack();

            TryClearPublisherAudioTrack();
            SetPublisherActiveAudioTrack(newAudioTrack);
        }

        private void OnVideoInputChanged(WebCamTexture webCamTexture)
        {
            if (_mediaInputProvider.VideoInput == null)
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

        private void CreatePublisherAudioTransceiver()
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

        private void CreatePublisherCustomVideoTransceiver(RenderTexture source, uint frameRate)
        {
            var videoTransceiverInit = BuildTransceiverInit(_peerType, TrackKind.Video, new PublisherVideoSettings()
            {
                MaxResolution = new VideoResolution(source.width, source.height),
                FrameRate = frameRate
            });

            var mediaStream = new MediaStream();
            var videoTrack = new VideoStreamTrack(source);
            mediaStream.AddTrack(videoTrack);

            videoTransceiverInit.streams = new[] { mediaStream };

            var transceiver = _peerConnection.AddTransceiver(videoTrack, videoTransceiverInit);
            _transceiverToMediaStream.Add(transceiver, mediaStream);
            
#if STREAM_DEBUG_ENABLED
            _logs.Warning($"Added custom video transceiver. Media stream ID: {mediaStream.Id}, track ID: {videoTrack.Id}");
#endif

            ForceCodec(transceiver, VideoCodecKeyH264, TrackKind.Video);
        }

        private void ReplaceActiveVideoTrack(VideoStreamTrack videoTrack)
        {
            PublisherVideoMediaStream.AddTrack(videoTrack);

            _videoTransceiver.Sender.ReplaceTrack(videoTrack);

            PublisherVideoTrack = videoTrack;
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
            var graphicsFormat = WebRTC.GetSupportedGraphicsFormat(gfxType);

            var res = GetPublisherResolution();
            _publisherVideoTrackTexture = new RenderTexture((int)res.Width, (int)res.Height, 0, format);


#if STREAM_DEBUG_ENABLED
            Debug.LogWarning(
                $"CreatePublisherVideoTrack, isPlaying: {_mediaInputProvider.VideoInput.isPlaying}, readable: " +
                $"{_mediaInputProvider.VideoInput.isReadable}, expectedGraphicsFormat: {graphicsFormat}, " +
                $"givenGraphicsFormat: {_mediaInputProvider.VideoInput.graphicsFormat}");
#endif

            return new VideoStreamTrack(_publisherVideoTrackTexture);
        }

        private AudioStreamTrack CreatePublisherAudioTrack() => new AudioStreamTrack(_mediaInputProvider.AudioInput);

        private void ForceCodec(RTCRtpTransceiver transceiver, string codecKey, TrackKind kind)
        {
            var capabilities = RTCRtpSender.GetCapabilities(kind);
            var forcedCodecs = capabilities.codecs.Where(c
                => c.mimeType.IndexOf(codecKey, StringComparison.OrdinalIgnoreCase) != -1);

#if STREAM_DEBUG_ENABLED
            var availableCodecsLog = capabilities.codecs.Select(c => $"`{kind}`: {c.mimeType}");
            _logs.Info($"Available Codecs:\n " + string.Join("\n", availableCodecsLog));
#endif

            if (!forcedCodecs.Any())
            {
                var availableCodecs = string.Join(", ", capabilities.codecs.Select(c => c.mimeType));
                _logs.Error(
                    $"Tried to filter codecs by `{codecKey}` key and kind `{kind}` but no results were found. Available codecs: {availableCodecs}");
                return;
            }

#if STREAM_DEBUG_ENABLED
            var forcedCodecsLog = forcedCodecs.Select(c => $"`{kind}`: {c.mimeType}");
            _logs.Info($"Forced Codecs:\n " + string.Join("\n", forcedCodecsLog));
#endif

            var error = transceiver.SetCodecPreferences(forcedCodecs.ToArray());
            if (error != RTCErrorType.None)
            {
                _logs.Error($"Failed to set codecs for kind kind `{kind}` due to error: {error}");
            }
        }
    }
}