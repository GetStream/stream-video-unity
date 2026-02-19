#if UNITY_ANDROID && !UNITY_EDITOR
#define STREAM_NATIVE_AUDIO //Defined in multiple files
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core.Configs;
using StreamVideo.Core.Models;
using StreamVideo.Core.Models.Sfu;
using StreamVideo.Core.Sfu;
using StreamVideo.Core.Trace;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;
using StreamVideo.Libs.Utils;
using StreamVideo.v1.Sfu.Models;
using StreamVideo.v1.Sfu.Signal;
using Unity.WebRTC;
using UnityEngine;
using TrackType = StreamVideo.v1.Sfu.Models.TrackType;

namespace StreamVideo.Core.LowLevelClient
{
    internal class PublisherPeerConnection : PeerConnectionBase
    {
        public event Action<VideoStreamTrack> PublisherVideoTrackChanged;
        public event Action<AudioStreamTrack> PublisherAudioTrackChanged;

        public const ulong MaxPublishAudioBitrate = 500_000;
        public const ulong MaxPublishVideoBitrate = 1_200_000;

        public const ulong FullPublishVideoBitrate = 1_200_000;
        public const ulong HalfPublishVideoBitrate = MaxPublishVideoBitrate / 2;
        public const ulong QuarterPublishVideoBitrate = MaxPublishVideoBitrate / 4;

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

                SavePublishedTrackOrder(TrackType.Audio);

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

                SavePublishedTrackOrder(TrackType.Video);

                _publisherVideoTrack = value;
                PublisherVideoTrackChanged?.Invoke(_publisherVideoTrack);
            }
        }

        public RTCRtpSender VideoSender { get; private set; }

        public IEnumerable<TrackType> PublishedTrackOrder => _publishedTrackOrder;

        // Full: 704×576  -> half: 352×288 -> quarter: 176×144 <- We want the smallest resolution to be above 96x96
        public static VideoResolution MinimumSafeTargetResolution => new VideoResolution(704, 576);

        public PublisherPeerConnection(ILogs logs, IEnumerable<ICEServer> iceServers,
            IMediaInputProvider mediaInputProvider, IStreamAudioConfig audioConfig,
            PublisherVideoSettings publisherVideoSettings, ISfuClient sfuClient, Tracer tracer, ISerializer serializer)
            : base(logs, StreamPeerType.Publisher, iceServers, tracer, serializer, sfuClient)
        {
            _mediaInputProvider = mediaInputProvider ?? throw new ArgumentNullException(nameof(mediaInputProvider));
            _audioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));
            _publisherVideoSettings = publisherVideoSettings ??
                                      throw new ArgumentNullException(nameof(publisherVideoSettings));

            _mediaInputProvider.AudioInputChanged += OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged += OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged += OnVideoInputChanged;

            _mediaInputProvider.PublisherAudioTrackIsEnabledChanged += OnPublisherAudioTrackIsEnabledChanged;
            _mediaInputProvider.PublisherVideoTrackIsEnabledChanged += OnPublisherVideoTrackIsEnabledChanged;
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
        public async Task InitPublisherTracksAsync()
        {
            // Create both transceivers WITHOUT individual negotiations to avoid concurrent negotiate calls
            // which would cause "m-lines order mismatch" errors
            ReplacePublisherAudioTrack(negotiate: false);
            ReplacePublisherVideoTrack(negotiate: false);

            // Only negotiate if at least one transceiver was created
            // Otherwise, SFU rejects the empty SetPublisher request with RequestValidationFailed
            if (_audioTransceiver != null || _videoTransceiver != null)
            {
                await Negotiate();
            }
            
            // StreamTodo: VideoSceneInput is not handled
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
            
            if (_negotiateRequested && !_isNegotiating)
            {
                var iceRestart = _negotiateRequestedWithIceRestart;
                _negotiateRequested = false;
                _negotiateRequestedWithIceRestart = false;
                Negotiate(iceRestart).LogIfFailed();
            }
        }

        public override Task RestartIce()
        {
            Logs.InfoIfDebug($"[{PeerType}] Restarting ICE connection");
            
            // The JS client ignores the RestartIce if SignalingState == RTCSignalingState.HaveLocalOffer
            // But rollback doesn't work in Unity's webrtc package, so we skip the rollback and allow ICE restart despite HaveLocalOffer state
            if (IsIceRestarting)
            {
                Logs.InfoIfDebug($"[{PeerType}] ICE restart is already in progress");
                return Task.CompletedTask;
            }
            
            if (SfuClient.CallState == CallingState.Reconnecting || SfuClient.CallState == CallingState.Joining)
            {
                Logs.InfoIfDebug($"[{PeerType}] Skipping ICE restart because CallState is {SfuClient.CallState}");
                return Task.CompletedTask;
            }

            return Negotiate(iceRestart: true);
        }

        //StreamTODO: Delete RtcSession.OnPublisherNegotiationNeeded
        private async Task Negotiate(bool iceRestart = false)
        {
            if (_isNegotiating)
            {
                _negotiateRequested = true;
                _negotiateRequestedWithIceRestart = _negotiateRequestedWithIceRestart || iceRestart;
                Logs.WarningIfDebug($"[{PeerType}][Negotiate] Already negotiating — flagged for re-negotiate after current one completes (iceRestart: {iceRestart})");
                return;
            }
            
            _isNegotiating = true;
            _negotiateRequested = false;
            _negotiateRequestedWithIceRestart = false;
            
            Logs.WarningIfDebug($"[{PeerType}][Negotiate] Started. Call state: {SfuClient.CallState}, Is PC Healthy: {IsHealthy}, PC Connection State: {PeerConnection.ConnectionState}, ICE conn state: {PeerConnection.IceConnectionState}");
            var sessionVersionAtStart = SfuClient.SessionVersion;
            
            try
            {
                var options = new RTCOfferAnswerOptions
                {
                    iceRestart = iceRestart
                };

                IsIceRestarting = iceRestart;

                // 1. Create offer
                var offer = await PeerConnection.CreateOfferAsync(ref options, GetCurrentCancellationTokenOrDefault());

                // 2. Set local description
                await PeerConnection.SetLocalDescriptionAsync(ref offer, GetCurrentCancellationTokenOrDefault());

                var tracks = GetAnnouncedTracks(offer.sdp);
                var request = new SetPublisherRequest
                {
                    Sdp = offer.sdp,
                    SessionId = SfuClient.SessionId,
                };
                request.Tracks.AddRange(tracks);

                var serializedRequest = "Only available in debug mode";
#if STREAM_DEBUG_ENABLED
                serializedRequest = Serializer.Serialize(request);
                Logs.Warning(
                    $"[{PeerType}][Negotiate] SetPublisherRequest (Tracks: {tracks.Count()}, SessionID: {SfuClient.SessionId}):\n{serializedRequest}");
#endif

                //StreamTODO: add cancellation token support
                // 3. Send SetPublisher request to get the SFU SDP answer
                var result = await SfuClient.RpcCallAsync(request, GeneratedAPI.SetPublisher,
                    nameof(GeneratedAPI.SetPublisher),
                    GetCurrentCancellationTokenOrDefault(), response => response.Error);

                // Check if session changed during the RPC call - if so, result is stale
                if (SfuClient.SessionVersion != sessionVersionAtStart)
                {
                    Logs.InfoIfDebug(
                        $"[{PeerType}][Negotiate] Negotiate result is stale - session version changed from {sessionVersionAtStart} to {SfuClient.SessionVersion}");
                    return;
                }

#if STREAM_DEBUG_ENABLED
                Logs.Warning($"[{PeerType}][Negotiate] RemoteDesc (SDP Answer):\n{result.Sdp}");
#endif

                if (result.Error != null)
                {
                    //StreamTODO: create custom exception
                    throw new Exception(
                        $"{nameof(GeneratedAPI.SetPublisher)} request failed with: {result.Error.Code}, {result.Error.Message}.\nRequest:\n{serializedRequest}");
                }

                try
                {
                    // 4. Set remote description
                    await SetRemoteDescriptionAsync(new RTCSessionDescription()
                    {
                        type = RTCSdpType.Answer,
                        sdp = result.Sdp
                    }, GetCurrentCancellationTokenOrDefault());

                    AddPendingIceCandidates();
                }
                catch (Exception e)
                {
                    Tracer?.Trace(PeerConnectionTraceKey.NegotiateErrorSetRemoteDescription,
                        e.Message ?? "unknown");
                    throw;
                }
            }
            catch (Exception e)
            {
                if (SfuClient.SessionVersion != sessionVersionAtStart)
                {
                    Logs.InfoIfDebug(
                        $"[{PeerType}][Negotiate] Ignoring stale negotiate error - session version changed from {sessionVersionAtStart} to {SfuClient.SessionVersion}");
                    return;
                }

                Logs.ExceptionIfDebug(e);

                // Negotiation failed, rollback to the previous state
                if (SignalingState == RTCSignalingState.HaveLocalOffer)
                {
                    //StreamTODO: JS client does below but in Unity webrtc, SetLocalDescriptionAsync throws if sdp is null/empty 
                    // var rollbackDesc = new RTCSessionDescription
                    // {
                    //     type = RTCSdpType.Rollback
                    // };
                    // await PeerConnection.SetLocalDescriptionAsync(ref rollbackDesc,
                    //     GetCurrentCancellationTokenOrDefault());
                }

                throw;
            }
            finally
            {
                IsIceRestarting = false;
                _isNegotiating = false;
                Logs.WarningIfDebug($"[{PeerType}][Negotiate] Ended");
            }

            AddTrickledIceCandidates();
        }

        private void AddTrickledIceCandidates()
        {
            //StreamTODO: in JS this subscribes to ICE trickle. Figure out if our way is correct
        }

        private IEnumerable<TrackInfo> GetAnnouncedTracks(string sdp)
        {
            foreach (var transceiver in GetKnownTransceivers())
            {
                if (transceiver.Sender?.Track == null)
                {
                    var isSenderNull = transceiver.Sender == null;
                    Logs.ErrorIfDebug(
                        $"GetAnnouncedTracks skipped transceiver because track is empty. isSenderNull {isSenderNull}");
                    continue;
                }

                var trackInfo = GenerateTrackInfo(transceiver, sdp);
                if (trackInfo != null)
                {
                    yield return trackInfo;
                }
            }
        }
        
        private IEnumerable<RTCRtpTransceiver> GetKnownTransceivers()
        {
            // Return in published order to maintain m-line ordering
            foreach (var trackType in _publishedTrackOrder)
            {
                switch (trackType)
                {
                    case TrackType.Audio when _audioTransceiver != null:
                        yield return _audioTransceiver;
                        break;
                    case TrackType.Video when _videoTransceiver != null:
                        yield return _videoTransceiver;
                        break;
                }
            }
            
            //StreamTODO: this should not happen, add log
            if (_publishedTrackOrder.Count == 0)
            {
                if (_audioTransceiver != null) yield return _audioTransceiver;
                if (_videoTransceiver != null) yield return _videoTransceiver;
            }
        }

        /// <summary>
        /// Returns the list of published tracks for the reconnect flow
        /// </summary>
        public IEnumerable<TrackInfo> GetAnnouncedTracksForReconnect()
        {
            string sdp;
            try
            {
                // Throws exception if SDP was not set
                sdp = PeerConnection?.LocalDescription.sdp;
            }
            catch
            {
#if STREAM_DEBUG_ENABLED
                Logs.WarningIfDebug("GetAnnouncedTracksForReconnect - SDP was NULL");
#endif
                yield break;
            }

            //StreamTODO: skip tracks stopped due to a codec switch
            foreach (var t in GetAnnouncedTracks(sdp))
            {
                yield return t;
            }
        }

        private TrackInfo GenerateTrackInfo(RTCRtpTransceiver transceiver, string sdp)
        {
            var track = transceiver.Sender.Track;
            if (track == null)
            {
#if STREAM_DEBUG_ENABLED
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Error: Track was NULL. Transceiver dump:");
                sb.AppendLine(DebugObjectPrinter.PrintObject(transceiver));
                sb.AppendLine("SDP:");
                sb.AppendLine(sdp);
                Logs.Error(sb.ToString());
#endif
                return null;
            }

            var isTrackLive = (track.ReadyState == TrackState.Live);

            //StreamTODO: verify why this was needed in the past. The JS client just takes the track.id
            var trackId = transceiver.Sender.Track.Kind == TrackKind.Video
                ? ExtractVideoTrackId(sdp)
                : transceiver.Sender.Track.Id;

            var mid = ExtractMid(transceiver, sdp);

            var trackInfo = new TrackInfo
            {
                TrackId = trackId,
                TrackType = track.Kind.ToInternalEnum(),
                Mid = mid,
                Dtx = false, //StreamTODO: enable this option but test on multiple devices
                Stereo = false, //StreamTODO: implement stereo
                Red = false, //StreamTODO: enable this option but test on multiple devices
                Muted = !isTrackLive,
                //Codec = null, //StreamTODO: implement option to force a code. Beware that in in the current Unity webrtc package not every codec works with simulcast
                //PublishOptionId = 0 //StreamTODO implement publish options
            };

            if (track.Kind == TrackKind.Video)
            {
                var videoLayers = GetPublisherVideoLayers(transceiver.Sender.GetParameters().encodings);
                trackInfo.Layers.AddRange(videoLayers);
            }

            return trackInfo;
        }

        private IEnumerable<VideoLayer> GetPublisherVideoLayers(IEnumerable<RTCRtpEncodingParameters> encodings)
        {
#if STREAM_DEBUG_ENABLED
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("GetPublisherVideoLayers:");
#endif
            foreach (var encoding in encodings)
            {
                var scaleBy = encoding.scaleResolutionDownBy ?? 1.0;
                var resolution = GetLatestVideoSettings().MaxResolution;
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
            Logs.Warning(sb.ToString());
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

        private string ExtractMid(RTCRtpTransceiver transceiver, string sdp)
        {
            if (!string.IsNullOrEmpty(transceiver.Mid))
            {
                return transceiver.Mid;
            }

            //StreamTODO: 

#if STREAM_DEBUG_ENABLED
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Error: Track MID was NULL. Transceiver dump:");
            sb.AppendLine(DebugObjectPrinter.PrintObject(transceiver));
            sb.AppendLine("SDP:");
            sb.AppendLine(sdp);
            Logs.Error(sb.ToString());
#endif

            return null;
        }

        private string ExtractVideoTrackId(string sdp)
        {
            try
            {
                var lines = sdp.Split("\n");
                var mediaStreamRecord
                    = lines.Single(l => l.StartsWith($"a=msid:{PublisherVideoMediaStream.Id}"));
                var parts = mediaStreamRecord.Split(" ");
                var result = parts[1];

                // StreamTodo: verify if this is needed
                result = result.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

                return result;
            }
            catch (Exception e)
            {
                using (new StringBuilderPoolScope(out var tempSb))
                {
                    tempSb.AppendLine($"Failed searching for: a=msid:{PublisherVideoMediaStream.Id}");
                    tempSb.AppendLine("In:");
                    tempSb.AppendLine(sdp);
                    tempSb.AppendLine("Error:");
                    tempSb.AppendLine(e.Message);
                    Logs.Error(tempSb.ToString());
                }

                throw;
            }
        }

        protected override void OnDisposing()
        {
            _mediaInputProvider.AudioInputChanged -= OnAudioInputChanged;
            _mediaInputProvider.VideoSceneInputChanged -= OnVideoSceneInputChanged;
            _mediaInputProvider.VideoInputChanged -= OnVideoInputChanged;

            _mediaInputProvider.PublisherAudioTrackIsEnabledChanged -= OnPublisherAudioTrackIsEnabledChanged;
            _mediaInputProvider.PublisherVideoTrackIsEnabledChanged -= OnPublisherVideoTrackIsEnabledChanged;

            ReleasePublisherVideoTrackTexture();

            TryClearPublisherAudioTrack();
            TryClearVideoTrack();
            
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

        private readonly List<TrackType> _publishedTrackOrder = new List<TrackType>();
        
        private bool _isNegotiating;
        private bool _negotiateRequested;
        private bool _negotiateRequestedWithIceRestart;

        private RenderTexture _publisherVideoTrackTexture;
        private VideoStreamTrack _publisherVideoTrack;
        private AudioStreamTrack _publisherAudioTrack;

        private RTCRtpTransceiver _videoTransceiver;
        private RTCRtpTransceiver _audioTransceiver;

        private void SavePublishedTrackOrder(TrackType type)
        {
            if (_publishedTrackOrder.Contains(type))
            {
                return;
            }

            _publishedTrackOrder.Add(type);
        }

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

                    yield return quarterQuality;
                    yield return halfQuality;
                    yield return fullQuality;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trackKind), trackKind, null);
            }
        }

        //StreamTODO: this should not be called when updating the track
        //Considering splitting into AddTransceiver, UpdateTransceiver
        private void CreatePublisherAudioTransceiverAndTrack(bool negotiate = true)
        {
            if (_audioTransceiver != null)
            {
                throw new InvalidOperationException("No audio transceiver");
            }

            var audioTransceiverInit = BuildTransceiverInit(TrackKind.Audio, _publisherVideoSettings);
            _audioTransceiver = PeerConnection.AddTransceiver(TrackKind.Audio, audioTransceiverInit);

            PublisherAudioMediaStream = new MediaStream();

            var audioTrack = CreatePublisherAudioTrack();
            SetPublisherActiveAudioTrack(audioTrack);

            if (_audioConfig.EnableRed)
            {
                ForceCodec(_audioTransceiver, AudioCodecKeyRed, TrackKind.Audio);
            }

            if (negotiate)
            {
                // StreamTODO: better handle async??
                Negotiate().LogIfFailed();
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
            Logs.WarningIfDebug("Local Audio Capture - STOP");
            PublisherAudioTrack.StopLocalAudioCapture();
#endif
            
            PublisherAudioTrack.Dispose();
            PublisherAudioTrack = null;
            
            Logs.WarningIfDebug("Removed Publisher Audio Track");
        }

        private void CreatePublisherVideoTransceiver(bool negotiate = true)
        {
            if (_videoTransceiver != null)
            {
                throw new InvalidOperationException("");
            }

            var videoTransceiverInit = BuildTransceiverInit(TrackKind.Video, _publisherVideoSettings);

            PublisherVideoMediaStream = new MediaStream();
            PublisherVideoTrack = CreatePublisherVideoTrack();

            PublisherVideoMediaStream.AddTrack(PublisherVideoTrack);

            // Order seems fragile here in order to get correct msid record in local offer with the PublisherVideoMediaStream
            videoTransceiverInit.streams = new[] { PublisherVideoMediaStream };

            _videoTransceiver = PeerConnection.AddTransceiver(PublisherVideoTrack, videoTransceiverInit);

            ForceCodec(_videoTransceiver, VideoCodecKeyH264, TrackKind.Video);

            VideoSender = _videoTransceiver.Sender;

            if (negotiate)
            {
                // StreamTODO: better handle async??
                Negotiate().LogIfFailed();
            }
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
                    $"Can't create publisher video track because `{nameof(_mediaInputProvider.VideoInput)}` is null");
            }

            ReleasePublisherVideoTrackTexture();

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
            ReleasePublisherVideoTrackTexture();

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
            Logs.WarningIfDebug($"[Audio] Created new AudioStreamTrack, enabled: {track.Enabled}, " + PrintConnectionState());
            return track;
        }

        private string PrintConnectionState()
            => $"PC Connection State: {PeerConnection.ConnectionState}, ICE Connection State: {PeerConnection.IceConnectionState}";

        private void ReleasePublisherVideoTrackTexture()
        {
            if (_publisherVideoTrackTexture == null)
            {
                return;
            }

            try
            {
                // Unity gives warning when releasing an active texture
                if (RenderTexture.active == _publisherVideoTrackTexture)
                {
                    RenderTexture.active = null;
                }
            }
            catch (Exception e)
            {
                Logs.WarningIfDebug(e.Message);
            }

            _publisherVideoTrackTexture.Release();
            _publisherVideoTrackTexture = null;
        }

        private void TryClearVideoTrack()
        {
            if (PublisherVideoTrack == null)
            {
                return;
            }

            PublisherVideoTrack.Stop();

            PublisherVideoMediaStream.RemoveTrack(PublisherVideoTrack);
            
            PublisherVideoTrack?.Dispose();
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
        /// <param name="negotiate">If true, triggers SDP negotiation after creating transceiver. 
        /// Set to false during InitPublisherTracks to avoid concurrent negotiations.</param>
        private void ReplacePublisherAudioTrack(bool negotiate = true)
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
                CreatePublisherAudioTransceiverAndTrack(negotiate);
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
        /// <param name="negotiate">If true, triggers SDP negotiation after creating transceiver. 
        /// Set to false during InitPublisherTracks to avoid concurrent negotiations.</param>
        private void ReplacePublisherVideoTrack(bool negotiate = true)
        {
            var isActive = _mediaInputProvider.VideoInput != null && _mediaInputProvider.PublisherVideoTrackIsEnabled;
            if (!isActive)
            {
                TryClearVideoTrack();
                return;
            }

            if (_videoTransceiver == null)
            {
                CreatePublisherVideoTransceiver(negotiate);
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
            Logs.Info($"Forced Codec of kind `{kind}`: " + string.Join(", ", forcedCodecs.Select(c => c.mimeType)));
#endif

            var error = transceiver.SetCodecPreferences(forcedCodecs.ToArray());
            if (error != RTCErrorType.None)
            {
                Logs.Error($"Failed to set codecs for kind `{kind}` due to error: {error}");
            }
        }
    }
}