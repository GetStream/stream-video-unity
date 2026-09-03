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
        private RectTransform _stage;

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
        private readonly List<ParticipantView> _layoutViews = new List<ParticipantView>();
        private GridLayoutGroup _stageGrid;
        private Vector2 _lastStageSize;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _stagePrepared;

        protected override void OnInit()
        {
            PrepareStage();

            _leaveBtn.onClick.AddListener(VideoManager.LeaveActiveCall);
            _endBtn.onClick.AddListener(VideoManager.EndActiveCall);

            _cameraPanel.Init(VideoManager.Client, UIManager);
            _microphonePanel.Init(VideoManager.Client, UIManager);
            
            _moreOptionsWindow.Init(VideoManager);
            _moreBtn.onClick.AddListener(_moreOptionsWindow.Show);

#if AUDIO_PROCESSING_ENABLED
            InitAudioProcessingUi();
#endif
        }

        protected override void OnShow(ShowArgs showArgs)
        {
            _activeCall = showArgs.ActiveCall;

            // If local user is the call owner we can "end" the call for all participants, otherwise we can only "leave" the call
            _endBtn.gameObject.SetActive(_activeCall.IsLocalUserOwner);

            // Generate participant UI for already present participants
            foreach (var participant in _activeCall.Participants)
            {
                AddParticipant(participant, applyLayout: false);
            }

            ApplyLayout();

            // Subscribe to participants joining or leaving the call
            _activeCall.ParticipantJoined += OnParticipantJoined;
            _activeCall.ParticipantLeft += OnParticipantLeft;

            // Subscribe to the change of the most actively speaking participant
            _activeCall.DominantSpeakerChanged += OnDominantSpeakerChanged;

            _activeCall.SortedParticipantsUpdated += ApplyLayout;
            _activeCall.LocalPreviewTextureChanged += OnLocalPreviewTextureChanged;

            UIManager.LocalCameraChanged += OnLocalCameraChanged;

            // Show active call ID so user can copy it and send others to join
            _joinCallIdInput.text = _activeCall.Id;
            
            // Notify child components
            _cameraPanel.NotifyParentShow();
            _microphonePanel.NotifyParentShow();
        }

        protected override void OnHide()
        {
            if (_activeCall != null)
            {
                _activeCall.ParticipantJoined -= OnParticipantJoined;
                _activeCall.ParticipantLeft -= OnParticipantLeft;
                _activeCall.DominantSpeakerChanged -= OnDominantSpeakerChanged;
                _activeCall.SortedParticipantsUpdated -= ApplyLayout;
                _activeCall.LocalPreviewTextureChanged -= OnLocalPreviewTextureChanged;
                _activeCall = null;
            }

            RemoveAllParticipants();

            UIManager.LocalCameraChanged -= OnLocalCameraChanged;
            
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

            ApplyLayout();
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
            => AddParticipant(participant, applyLayout: true);

        private void OnParticipantLeft(string sessionId, string userId)
            => RemoveParticipant(sessionId, userId, applyLayout: true);

        private void AddParticipant(IStreamVideoCallParticipant participant, bool applyLayout)
        {
            Debug.Log("Participant Joined. SessionID: " + participant.SessionId);
            var view = Instantiate(_participantViewPrefab, _stage);
            view.Init(participant, VideoManager);
            _participantSessionIdToView.Add(participant.SessionId, view);

            if (participant.IsLocalParticipant)
            {
                // Set input camera as a video source for local participant - we won't receive TrackAdded event for local participant
                view.SetLocalCameraSource(_activeCall.GetLocalPreviewTexture());
                //StreamTodo: this will invalidate each time WebCamTexture is internally replaced so we need a better way to expose this
            }

            if (applyLayout)
            {
                ApplyLayout();
            }
        }

        private void RemoveParticipant(string sessionId, string userId, bool applyLayout)
        {
            Debug.Log("Participant Left. SessionID: " + sessionId);
            if (!_participantSessionIdToView.TryGetValue(sessionId, out var view))
            {
                Debug.LogError("Failed to find view for removed participant with sessionId: " + sessionId);
                return;
            }

            _participantSessionIdToView.Remove(sessionId);
            Destroy(view.gameObject);

            if (applyLayout)
            {
                ApplyLayout();
            }
        }

        protected void Update()
        {
            if (_activeCall == null || _stage == null)
            {
                return;
            }

            var size = _stage.rect.size;
            if (size == _lastStageSize && Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            {
                return;
            }

            _lastStageSize = size;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            ApplyLayout();
        }

        private void PrepareStage()
        {
            if (_stagePrepared)
            {
                return;
            }

            _stagePrepared = true;

            if (_dominantSpeakerContainer != null && _dominantSpeakerContainer.parent != null)
            {
                _dominantSpeakerContainer.parent.gameObject.SetActive(false);
            }

            if (_remainingParticipantsContainer != null)
            {
                var viewport = _remainingParticipantsContainer.parent;
                var scroll = viewport != null ? viewport.parent : null;
                if (scroll != null)
                {
                    scroll.gameObject.SetActive(false);
                }
                else
                {
                    _remainingParticipantsContainer.gameObject.SetActive(false);
                }
            }

            if (_stage == null && _dominantSpeakerContainer != null)
            {
                var dominantParent = _dominantSpeakerContainer.parent;
                if (dominantParent != null)
                {
                    _stage = dominantParent.parent as RectTransform;
                }
            }

            if (_stage == null)
            {
                Debug.LogError("CallScreenView is missing the participant stage RectTransform.");
                return;
            }

            var stageLayout = _stage.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (stageLayout != null)
            {
                stageLayout.enabled = false;
            }

            _stageGrid = _stage.GetComponent<GridLayoutGroup>();
            if (_stageGrid == null)
            {
                _stageGrid = _stage.gameObject.AddComponent<GridLayoutGroup>();
            }

            _stageGrid.childAlignment = TextAnchor.MiddleCenter;
            _stageGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _stageGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _stageGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            var callLayout = GetComponent<VerticalLayoutGroup>();
            if (callLayout != null)
            {
                callLayout.childControlHeight = true;
                callLayout.childForceExpandHeight = true;
                callLayout.childControlWidth = true;
                callLayout.childForceExpandWidth = true;
            }

            var stageElement = _stage.GetComponent<LayoutElement>();
            if (stageElement == null)
            {
                stageElement = _stage.gameObject.AddComponent<LayoutElement>();
            }

            stageElement.flexibleHeight = 1f;
            stageElement.flexibleWidth = 1f;
            stageElement.minHeight = 0f;

            var controls = transform.Find("Controls");
            if (controls != null)
            {
                var controlsElement = controls.GetComponent<LayoutElement>();
                if (controlsElement == null)
                {
                    controlsElement = controls.gameObject.AddComponent<LayoutElement>();
                }

                controlsElement.minHeight = 100f;
                controlsElement.preferredHeight = 100f;
                controlsElement.flexibleHeight = 0f;
            }

            var moreOptions = transform.Find("MoreOptionsWindow");
            if (moreOptions != null)
            {
                var moreElement = moreOptions.GetComponent<LayoutElement>();
                if (moreElement == null)
                {
                    moreElement = moreOptions.gameObject.AddComponent<LayoutElement>();
                }

                moreElement.ignoreLayout = true;
            }
        }

        private void ApplyLayout()
        {
            if (_stage == null || _activeCall == null)
            {
                return;
            }

            _layoutViews.Clear();
            foreach (var participant in _activeCall.SortedParticipants)
            {
                if (!_participantSessionIdToView.TryGetValue(participant.SessionId, out var view))
                {
                    continue;
                }

                _layoutViews.Add(view);
                if (view.transform.parent != _stage)
                {
                    view.transform.SetParent(_stage, worldPositionStays: false);
                }
            }

            var count = _layoutViews.Count;
            if (_stageGrid == null || count == 0)
            {
                return;
            }

            var isPortrait = Screen.height >= Screen.width || _stage.rect.height >= _stage.rect.width;
            var cols = count == 1 || (count == 2 && isPortrait)
                ? 1
                : isPortrait ? 2 : Mathf.Min(3, count);
            var rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));

            _stageGrid.enabled = true;
            _stageGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _stageGrid.constraintCount = cols;
            _stageGrid.spacing = new Vector2(8f, 8f);

            var rect = _stage.rect;
            var spacing = _stageGrid.spacing;
            _stageGrid.cellSize = new Vector2(
                Mathf.Max(1f, (rect.width - spacing.x * (cols - 1)) / cols),
                Mathf.Max(1f, (rect.height - spacing.y * (rows - 1)) / rows));

            for (var i = 0; i < _layoutViews.Count; i++)
            {
                _layoutViews[i].transform.SetSiblingIndex(i);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_stage);
        }

        private void RemoveAllParticipants()
        {
            foreach (var (sessionId, view) in _participantSessionIdToView)
            {
                Destroy(view.gameObject);
            }

            _participantSessionIdToView.Clear();
        }

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
        
#if AUDIO_PROCESSING_ENABLED
        private void InitAudioProcessingUi()
        {
            if (_apmToggleBtn == null || _echoToggleBtn == null || _noiseToggleBtn == null ||
                _gainToggleBtn == null || _noiseLvlBtn == null)
            {
                return;
            }

            _apmToggleBtn.onClick.AddListener(OnApmToggleClicked);
            _echoToggleBtn.onClick.AddListener(OnEchoToggleClicked);
            _noiseToggleBtn.onClick.AddListener(OnNoiseToggleClicked);
            _gainToggleBtn.onClick.AddListener(OnGainToggleClicked);
            _noiseLvlBtn.onClick.AddListener(OnNoiseLvlClicked);

            _audioProcessingConfig = new AudioProcessingConfig(VideoManager.Client);
            _audioProcessingConfig.Updated += AudioProcessingConfigUpdated;
            _audioProcessingConfig.LoadCurrentConfig();
        }

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