#if UNITY_ANDROID && !UNITY_EDITOR
#define AUDIO_PROCESSING_ENABLED
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Devices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    /// <summary>
    /// Screen visible during the active call. Shows other participants
    /// </summary>
    public class CallScreenView : BaseScreenView<CallScreenView.ShowArgs>
    {
        /// <summary>
        /// Arguments required to initialize this screen when showing
        /// </summary>
        public readonly struct ShowArgs
        {
            public readonly IStreamCall ActiveCall;

            public ShowArgs(IStreamCall activeCall)
            {
                ActiveCall = activeCall;
            }
        }

        // Store participant views by SessionID so we can easily find it when they're leaving the call
        private readonly Dictionary<string, ParticipantView> _participantSessionIdToView
            = new Dictionary<string, ParticipantView>();

        [SerializeField]
        private ParticipantView _participantViewPrefab;

        [SerializeField]
        private Transform _dominantSpeakerContainer;

        [SerializeField]
        private Transform _remainingParticipantsContainer;

        [SerializeField]
        private Button _leaveBtn;

        [SerializeField]
        private Button _endBtn;

        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        [SerializeField]
        private CameraMediaDevicePanel _cameraPanel;

        [SerializeField]
        private MicrophoneMediaDevicePanel _microphonePanel;

        [SerializeField]
        private Button _apmToggleBtn;

        [SerializeField]
        private Button _echoToggleBtn;

        [SerializeField]
        private Button _gainToggleBtn;

        [SerializeField]
        private Button _noiseToggleBtn;

        [SerializeField]
        private Button _noiseLvlBtn;
        
        [SerializeField]
        private Button _moreBtn;
        
        [SerializeField]
        private MoreOptionsWindowView _moreOptionsWindow;

        private IStreamCall _activeCall;
        private ParticipantView _currentDominantSpeakerView;
        private AudioProcessingConfig _audioProcessingConfig;

        protected override void OnInit()
        {
            _leaveBtn.onClick.AddListener(VideoManager.LeaveActiveCall);
            _endBtn.onClick.AddListener(VideoManager.EndActiveCall);

            _cameraPanel.Init(VideoManager.Client, UIManager);
            _microphonePanel.Init(VideoManager.Client, UIManager);
            
            _moreOptionsWindow.Init(VideoManager);
            _moreBtn.onClick.AddListener(_moreOptionsWindow.Show);

#if AUDIO_PROCESSING_ENABLED
            // Landscape CallScreen leaves these unassigned; portrait CallScreenPortrait wires them.
            if (_apmToggleBtn != null && _echoToggleBtn != null && _noiseToggleBtn != null &&
                _gainToggleBtn != null && _noiseLvlBtn != null)
            {
                _apmToggleBtn.onClick.AddListener(OnApmToggleClicked);
                _echoToggleBtn.onClick.AddListener(OnEchoToggleClicked);
                _noiseToggleBtn.onClick.AddListener(OnNoiseToggleClicked);
                _gainToggleBtn.onClick.AddListener(OnGainToggleClicked);
                _noiseLvlBtn.onClick.AddListener(OnNoiseLvlClicked);

                _audioProcessingConfig = new AudioProcessingConfig(VideoManager.Client);
                _audioProcessingConfig.Updated += AudioProcessingConfigUpdated;

                _audioProcessingConfig.LoadCurrentConfig();
            }
#endif
        }

        protected override void OnShow(ShowArgs showArgs)
        {
            _activeCall = showArgs.ActiveCall;

#if STREAM_DEBUG_ENABLED
            LogUiRotate(
                $"OnShow parent={transform.parent?.name} screen={name} call={_activeCall.Id} " +
                $"participants={_activeCall.Participants.Count} {Screen.width}x{Screen.height} {Screen.orientation} " +
                $"dominant={DescribeRect(_dominantSpeakerContainer)} remaining={DescribeRect(_remainingParticipantsContainer)}");
#endif

            // If local user is the call owner we can "end" the call for all participants, otherwise we can only "leave" the call
            _endBtn.gameObject.SetActive(_activeCall.IsLocalUserOwner);

            // Adopt existing participant views (rotation) or create them (join)
            foreach (var participant in _activeCall.Participants)
            {
                AddParticipant(participant, sortParticipantViews: false);
            }

            SortParticipantViews();

            // Subscribe to participants joining or leaving the call
            _activeCall.ParticipantJoined += OnParticipantJoined;
            _activeCall.ParticipantLeft += OnParticipantLeft;

            // Subscribe to the change of the most actively speaking participant
            _activeCall.DominantSpeakerChanged += OnDominantSpeakerChanged;

            _activeCall.SortedParticipantsUpdated += SortParticipantViews;
            _activeCall.LocalPreviewTextureChanged += OnLocalPreviewTextureChanged;

            UIManager.LocalCameraChanged += OnLocalCameraChanged;

            // Show active call ID so user can copy it and send others to join
            _joinCallIdInput.text = _activeCall.Id;

            // Notify child components
            _cameraPanel.NotifyParentShow();
            _microphonePanel.NotifyParentShow();

#if AUDIO_PROCESSING_ENABLED
            _audioProcessingConfig?.LoadCurrentConfig();
#endif
        }

        protected override void OnHide()
        {
#if STREAM_DEBUG_ENABLED
            LogUiRotate($"OnHide parent={transform.parent?.name} screen={name}");
#endif
            if (_activeCall != null)
            {
                _activeCall.ParticipantJoined -= OnParticipantJoined;
                _activeCall.ParticipantLeft -= OnParticipantLeft;
                _activeCall.DominantSpeakerChanged -= OnDominantSpeakerChanged;
                _activeCall.SortedParticipantsUpdated -= SortParticipantViews;
                _activeCall.LocalPreviewTextureChanged -= OnLocalPreviewTextureChanged;
                _activeCall = null;
            }

            // Views are owned by UIManager and moved between layouts. Do not destroy them here.
            _participantSessionIdToView.Clear();

            UIManager.LocalCameraChanged -= OnLocalCameraChanged;

            if (_moreOptionsWindow != null)
            {
                _moreOptionsWindow.Hide();
            }

            // Notify child components
            _cameraPanel.NotifyParentHide();
            _microphonePanel.NotifyParentHide();
        }

        private void OnDominantSpeakerChanged(IStreamVideoCallParticipant currentDominantSpeaker,
            IStreamVideoCallParticipant previousDominantSpeaker)
        {
            Debug.Log(
                $"Dominant speaker changed from: `{GetSpeakerName(previousDominantSpeaker)}` to: `{GetSpeakerName(currentDominantSpeaker)}`");

            foreach (var participantView in _participantSessionIdToView.Values)
            {
                var isDominantSpeaker = participantView.Participant == currentDominantSpeaker;
                participantView.UpdateIsDominantSpeaker(isDominantSpeaker);
            }

            SortParticipantViews();
        }

        private static string GetSpeakerName(IStreamVideoCallParticipant participant)
        {
            if (participant == null)
            {
                return "None";
            }

            return string.IsNullOrEmpty(participant.Name) ? participant.UserId : participant.Name;
        }

        private void OnParticipantJoined(IStreamVideoCallParticipant participant)
            => AddParticipant(participant, sortParticipantViews: true);

        private void OnParticipantLeft(string sessionId, string userId)
            => RemoveParticipant(sessionId, userId, sortParticipantViews: true);

        private void AddParticipant(IStreamVideoCallParticipant participant, bool sortParticipantViews)
        {
            Debug.Log("Participant Joined. SessionID: " + participant.SessionId);
            var parent = GetParticipantViewParent(participant);
            var view = UIManager.GetOrCreateParticipantView(participant, parent, _participantViewPrefab);
            _participantSessionIdToView[participant.SessionId] = view;
#if STREAM_DEBUG_ENABLED
            LogUiRotate(
                $"AddParticipant session={participant.SessionId} local={participant.IsLocalParticipant} " +
                $"parent={parent?.name} viewParent={view.transform.parent?.name} set={transform.parent?.name}");
#endif

            if (participant.IsLocalParticipant)
            {
                // Set input camera as a video source for local participant - we won't receive TrackAdded event for local participant
                view.SetLocalCameraSource(_activeCall.GetLocalPreviewTexture());
            }

            if (sortParticipantViews)
            {
                SortParticipantViews();
            }
        }

        private void RemoveParticipant(string sessionId, string userId, bool sortParticipantViews)
        {
            Debug.Log("Participant Left. SessionID: " + sessionId);
            _participantSessionIdToView.Remove(sessionId);
            UIManager.DestroyParticipantView(sessionId);

            if (sortParticipantViews)
            {
                SortParticipantViews();
            }
        }

        /// <summary>
        /// Sort participant views based on SortedParticipants property.
        /// This will place dominant participant in large window and the other participants in a scrollable view underneath
        /// </summary>
        private void SortParticipantViews()
        {
            var index = 0;
            foreach (var participantView in _participantSessionIdToView.Values)
            {
                var isDominantSpeaker = participantView.Participant == _activeCall.DominantSpeaker;
                var parent = GetParticipantViewParent(isDominantSpeaker);

                participantView.transform.SetParent(parent, worldPositionStays: false);
                participantView.transform.localScale = Vector3.one;
                participantView.transform.localRotation = Quaternion.identity;

                if (!isDominantSpeaker)
                {
                    // Set valid order of the view relative to other views. We skip this for dominant speaker because he's under a different parent Transform
                    participantView.transform.SetSiblingIndex(index);
                    index++;
                }
            }
        }

        private Transform GetParticipantViewParent(IStreamVideoCallParticipant participant)
        {
            var isDominantSpeaker = participant == _activeCall.DominantSpeaker;
            return GetParticipantViewParent(isDominantSpeaker);
        }

        private Transform GetParticipantViewParent(bool isDominantSpeaker)
            => isDominantSpeaker ? _dominantSpeakerContainer : _remainingParticipantsContainer;

        private void OnLocalCameraChanged(WebCamTexture activeCamera)
            => RefreshLocalPreview();

        private void OnLocalPreviewTextureChanged(Texture previewTexture)
            => RefreshLocalPreview();

        private void RefreshLocalPreview()
        {
            var localParticipant
                = _participantSessionIdToView.Values.FirstOrDefault(p => p.Participant.IsLocalParticipant);
            if (localParticipant == null || _activeCall == null)
            {
                return;
            }

            localParticipant.SetLocalCameraSource(_activeCall.GetLocalPreviewTexture());
        }

#if STREAM_DEBUG_ENABLED
        private static void LogUiRotate(string message)
        {
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "[UIRotate] {0}", message);
        }

        private static string DescribeRect(Transform target)
        {
            if (target == null)
            {
                return "null";
            }

            var rectTransform = target as RectTransform;
            if (rectTransform == null)
            {
                return target.name;
            }

            var rect = rectTransform.rect;
            return $"{target.name} {rect.width:0}x{rect.height:0} hier={target.gameObject.activeInHierarchy}";
        }
#endif
        
#if AUDIO_PROCESSING_ENABLED
        private void OnNoiseLvlClicked()
        {
            _audioProcessingConfig.NoiseLvl++;
            _audioProcessingConfig.Apply();
            _audioProcessingConfig.LoadCurrentConfig();
        }

        private void OnNoiseToggleClicked()
        {
            _audioProcessingConfig.NoiseEnabled = !_audioProcessingConfig.NoiseEnabled;
            _audioProcessingConfig.Apply();
            _audioProcessingConfig.LoadCurrentConfig();
        }

        private void OnGainToggleClicked()
        {
            _audioProcessingConfig.AutoGainEnabled = !_audioProcessingConfig.AutoGainEnabled;
            _audioProcessingConfig.Apply();
            _audioProcessingConfig.LoadCurrentConfig();
        }

        private void OnEchoToggleClicked()
        {
            _audioProcessingConfig.EchoEnabled = !_audioProcessingConfig.EchoEnabled;
            _audioProcessingConfig.Apply();
            _audioProcessingConfig.LoadCurrentConfig();
        }

        private void OnApmToggleClicked()
        {
            _audioProcessingConfig.Enabled = !_audioProcessingConfig.Enabled;
            _audioProcessingConfig.Apply();
            _audioProcessingConfig.LoadCurrentConfig();
        }
        
        void AudioProcessingConfigUpdated()
        {
            if (_apmToggleBtn == null)
            {
                return;
            }

            var apmOn = _audioProcessingConfig.Enabled;

            try
            {
                _apmToggleBtn.gameObject.GetComponentInChildren<TMP_Text>().text = apmOn ? "APM: ON" : "APM: OFF";
                _apmToggleBtn.gameObject.GetComponent<Image>().color = apmOn ? Color.green : Color.red;
            
                _echoToggleBtn.gameObject.GetComponentInChildren<TMP_Text>().text = _audioProcessingConfig.EchoEnabled ? "Echo: ON" : "Echo: OFF";
                _echoToggleBtn.gameObject.GetComponent<Image>().color = _audioProcessingConfig.EchoEnabled ? Color.green : Color.red;
            
            
                _noiseToggleBtn.gameObject.GetComponentInChildren<TMP_Text>().text = _audioProcessingConfig.NoiseEnabled ? "Noise - ON" : "Noise - OFF";
                _noiseToggleBtn.gameObject.GetComponent<Image>().color = _audioProcessingConfig.NoiseEnabled ? Color.green : Color.red;
            
                _gainToggleBtn.gameObject.GetComponentInChildren<TMP_Text>().text = _audioProcessingConfig.AutoGainEnabled ? "Auto Gain: ON" : "Auto Gain: OFF";
                _gainToggleBtn.gameObject.GetComponent<Image>().color = _audioProcessingConfig.AutoGainEnabled ? Color.green : Color.red;
            
                _noiseLvlBtn.gameObject.GetComponentInChildren<TMP_Text>().text = "Noise Lvl: " + _audioProcessingConfig.NoiseLvl;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
#endif
    }
}