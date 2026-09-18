using System;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI
{
    public class ParticipantView : MonoBehaviour
    {
        public IStreamVideoCallParticipant Participant { get; private set; }

        public void Init(IStreamVideoCallParticipant participant, StreamVideoManager videoManager)
        {
            _videoManager = videoManager ?? throw new ArgumentNullException(nameof(videoManager));
            
            if (Participant != null)
            {
                throw new NotSupportedException("reusing participant view for new participant is not supported yet");
            }

            Participant = participant ?? throw new ArgumentNullException(nameof(participant));
            
            OnIsSpeakingChanged(Participant.IsSpeaking);
            OnAudioLevelChanged(Participant.AudioLevel);
            
            Participant.TrackAdded += OnParticipantTrackAdded;
            Participant.AudioLevelChanged += OnAudioLevelChanged;
            Participant.IsSpeakingChanged += OnIsSpeakingChanged;

            _name.text = Participant.Name;
        }

        public void UpdateIsDominantSpeaker(bool isDominantSpeaker)
        {
            var frameColor = isDominantSpeaker ? _dominantSpeakerFrameColor : _defaultSpeakerFrameColor;
            _videoFrame.color = frameColor;
        }

        /// <summary>
        /// Call this for local participant only. We will not receive the `Participant.TrackAdded` event for the local participant.
        /// So in order to show the stream from a local camera we hook it up separately
        /// </summary>
        public void SetLocalCameraSource(WebCamTexture localWebCamTexture)
            => SetLocalCameraSource((Texture)localWebCamTexture);

        public void SetLocalCameraSource(Texture localCameraTexture)
        {
            _video.texture = localCameraTexture;
        }
        
        // Called by Unity Engine
        protected void Awake()
        {
            _videoRectTransform = _video.GetComponent<RectTransform>();
            _baseVideoRotation = _videoRectTransform.rotation;
            _muteLocallyToggleButton.onClick.AddListener(OnMuteLocallyToggleClicked);
        }

        // Called by Unity Engine
        protected void Update()
        {
            var rect = _videoRectTransform.rect;
            var videoRenderedSize = new Vector2(rect.width, rect.height);
            var forcedRequestedSize = new Vector2(_forceRequestedResolutionWidth, _forceRequestedResolutionHeight);
            var finalRequestedSize = _forceRequestedResolution ? forcedRequestedSize : videoRenderedSize;
            
            if (_lastRequestedResolution != finalRequestedSize)
            {
                _lastRequestedResolution = finalRequestedSize;
                var videoResolution = new VideoResolution((int)finalRequestedSize.x, (int)finalRequestedSize.y);
                
                // To optimize bandwidth we always request the video resolution that matches what we're actually rendering
                Participant.UpdateRequestedVideoResolution(videoResolution);
                Debug.Log($"Rendered resolution changed for participant `{Participant.UserId}`. Requested video resolution update to: {videoResolution}");
            }

            FixVideoOrientation();
        }

        /// <summary>
        /// Mobile users can either stream in landscape mode or portrait mode. We must rotate the video texture to match the orientation of the device.
        /// </summary>
        private void FixVideoOrientation()
        {
            var remoteAngle = 0;
            var localAngle = 0;
            Texture previewTex = _video != null ? _video.texture : null;

            // For remote users we have their video track -> fix rotation based on the video track rotation angle
            if (Participant != null && Participant.VideoTrack != null && Participant.VideoTrack is StreamVideoTrack streamVideoTrack)
            {
                remoteAngle = streamVideoTrack.VideoRotationAngle;
                _videoRectTransform.rotation = _baseVideoRotation * Quaternion.AngleAxis(-remoteAngle, Vector3.forward);
            }
            
            // Local preview may be a compositor RT; rotation still comes from the camera, not the RT.
            if (Participant != null && Participant.IsLocalParticipant)
            {
                var sourceWebCamTexture = _videoManager != null
                    ? _videoManager.Client.VideoDeviceManager.GetSelectedDeviceWebCamTexture()
                    : _video.texture as WebCamTexture;
                if (sourceWebCamTexture == null || !sourceWebCamTexture.isPlaying || sourceWebCamTexture.width <= 16)
                {
                    return;
                }

                localAngle = sourceWebCamTexture.videoRotationAngle;
                _videoRectTransform.rotation = _baseVideoRotation * Quaternion.AngleAxis(-localAngle, Vector3.forward);
#if STREAM_DEBUG_ENABLED
                LogOrientationDebug(sourceWebCamTexture, previewTex, localAngle, remoteAngle, isLocal: true);
#endif
                return;
            }

#if STREAM_DEBUG_ENABLED
            LogOrientationDebug(previewTex as WebCamTexture, previewTex, localAngle, remoteAngle, isLocal: false);
#endif
        }

#if STREAM_DEBUG_ENABLED
        private void LogOrientationDebug(WebCamTexture webcam, Texture preview, int localAngle, int remoteAngle,
            bool isLocal)
        {
            var rect = _videoRectTransform != null ? _videoRectTransform.rect : Rect.zero;
            var texW = preview != null ? preview.width : 0;
            var texH = preview != null ? preview.height : 0;
            var appliedZ = _videoRectTransform != null ? _videoRectTransform.eulerAngles.z : 0f;
            var swap = Mathf.Abs(localAngle) % 180 == 90 || Mathf.Abs(remoteAngle) % 180 == 90;
            var texAspect = texH > 0 ? texW / (float)texH : 0f;
            var rectAspect = rect.height > 0.001f ? rect.width / rect.height : 0f;
            var expectedAspectAfterRot = swap && texH > 0 ? texH / (float)texW : texAspect;
            var payload = "[BgFilterOrient] ui.preview"
                + " | local=" + isLocal
                + " screen=" + Screen.width + "x" + Screen.height + " " + Screen.orientation
                + " tex=" + texW + "x" + texH + " type=" + (preview != null ? preview.GetType().Name : "null")
                + " rect=" + rect.width.ToString("0") + "x" + rect.height.ToString("0")
                + " texAspect=" + texAspect.ToString("0.000")
                + " rectAspect=" + rectAspect.ToString("0.000")
                + " expectedAspectAfterRot=" + expectedAspectAfterRot.ToString("0.000")
                + " aspectMismatch=" + (Mathf.Abs(rectAspect - expectedAspectAfterRot) > 0.05f)
                + " localAngle=" + localAngle
                + " remoteAngle=" + remoteAngle
                + " appliedZ=" + appliedZ.ToString("0.0")
                + (webcam != null
                    ? " webcam=" + webcam.width + "x" + webcam.height
                      + " rot=" + webcam.videoRotationAngle
                      + " mirrored=" + webcam.videoVerticallyMirrored
                      + " frontDevice=" + IsFrontFacing(webcam)
                    : "");
            if (payload == _lastOrientationDebug)
            {
                return;
            }

            _lastOrientationDebug = payload;
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", payload);
        }

        private static bool IsFrontFacing(WebCamTexture webcam)
        {
            var devices = WebCamTexture.devices;
            for (var i = 0; i < devices.Length; i++)
            {
                if (devices[i].name == webcam.deviceName)
                {
                    return devices[i].isFrontFacing;
                }
            }

            return false;
        }

        private string _lastOrientationDebug;
#endif

        // Called by Unity Engine
        protected void OnDestroy()
        {
            if (Participant != null)
            {
                Participant.TrackAdded -= OnParticipantTrackAdded;
                Participant.AudioLevelChanged -= OnAudioLevelChanged;
                Participant.IsSpeakingChanged -= OnIsSpeakingChanged;
            }
        }

        [SerializeField]
        private TMP_Text _name;

        [SerializeField]
        private RawImage _video;
        
        [SerializeField]
        private RawImage _videoFrame;
        
        [SerializeField]
        private Color32 _dominantSpeakerFrameColor;
        
        [SerializeField]
        private Color32 _defaultSpeakerFrameColor;

        [SerializeField]
        private bool _forceRequestedResolution = false;
        
        [SerializeField]
        private int _forceRequestedResolutionWidth = 300;
        
        [SerializeField]
        private int _forceRequestedResolutionHeight = 300;

        [SerializeField]
        private GameObject _isMutedIcon;

        [SerializeField]
        private Button _muteLocallyToggleButton;
        
        [SerializeField]
        private TMP_Text _audioLevel;
        
        [SerializeField]
        private GameObject _isSpeakingIcon;
        
        private AudioSource _audioSource;
        private RectTransform _videoRectTransform;
        private Vector2 _lastRequestedResolution;
        private Quaternion _baseVideoRotation;
        private StreamVideoManager _videoManager;

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
            Debug.Log($"Track received from `{participant.UserId}`, type: {track.GetType()}");
            switch (track)
            {
                case StreamAudioTrack streamAudioTrack:
                    if (_audioSource != null)
                    {
                        // A new track for the same participant can be received after reconnecting
                        Destroy(_audioSource);
                        _audioSource = null;
                    }

                    _audioSource = gameObject.AddComponent<AudioSource>();
                    streamAudioTrack.SetAudioSourceTarget(_audioSource);

                    // Apply cached local mute state in case participant rejoined
                    if (_videoManager.IsParticipantMutedLocally(participant))
                    {
                        streamAudioTrack.MuteLocally();
                        UpdateMuteIcon();
                    }
                    
                    break;

                case StreamVideoTrack streamVideoTrack:
                    streamVideoTrack.SetRenderTarget(_video);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(track));
            }
        }
        
        private void OnMuteLocallyToggleClicked()
        {
            if (Participant == null)
            {
                return;
            }

            var isMuted = _videoManager.IsParticipantMutedLocally(Participant);
            var newIsMuted = !isMuted;

            var actionLabel = newIsMuted ? "Muted" : "Unmuted";
            Debug.Log(actionLabel + " participant with user ID: " + Participant.UserId);

            if (newIsMuted)
            {
                _videoManager.MuteLocally(Participant);
            }
            else
            {
                _videoManager.UnmuteLocally(Participant);
            }

            UpdateMuteIcon();
        }

        private void UpdateMuteIcon()
        {
            var isMuted = _videoManager.IsParticipantMutedLocally(Participant);
            _isMutedIcon.SetActive(isMuted);
        }
        
        private void OnIsSpeakingChanged(bool isSpeaking)
        {
            _isSpeakingIcon.SetActive(isSpeaking);
        }

        private void OnAudioLevelChanged(float audioLevel)
        {
            _audioLevel.text = audioLevel.ToString();
        }
    }
}