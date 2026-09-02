using System;
using StreamVideo.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    public class MoreOptionsWindowView : MonoBehaviour
    {
        public void Init(StreamVideoManager streamVideoManager)
        {
            _streamVideoManager = streamVideoManager ?? throw new ArgumentNullException(nameof(streamVideoManager));
            
            _hideBtn.onClick.AddListener(OnCloseButtonClicked);
            
            _muteSelfAudioBtn.onClick.AddListener(() => _streamVideoManager.ActiveCall?.MuteSelf(audio: true, video: false, screenShare: false));
            _muteOthersAudioBtn.onClick.AddListener(() => _streamVideoManager.ActiveCall?.MuteOthers(audio: true, video: false, screenShare: false));
            _muteSelfVideoBtn.onClick.AddListener(() => _streamVideoManager.ActiveCall?.MuteSelf(audio: false, video: true, screenShare: false));
            _muteOthersVideoBtn.onClick.AddListener(() => _streamVideoManager.ActiveCall?.MuteOthers(audio: false, video: true, screenShare: false));
            _toggleMusicBtn.onClick.AddListener(() => _streamVideoManager.ToggleMusic());
            AddBackgroundFilterToggle();
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        protected void Awake()
        {
            Hide();
        }

        protected void OnDestroy()
        {
            _hideBtn.onClick.RemoveListener(OnCloseButtonClicked);
        }

        [SerializeField]
        private Button _muteSelfAudioBtn;

        [SerializeField]
        private Button _muteOthersAudioBtn;
        
        [SerializeField]
        private Button _muteSelfVideoBtn;

        [SerializeField]
        private Button _muteOthersVideoBtn;
        
        [SerializeField]
        private Button _toggleMusicBtn;

        [SerializeField]
        private Button _hideBtn;

        private StreamVideoManager _streamVideoManager;
        private TMP_Text _backgroundFilterLabel;

        private void OnCloseButtonClicked() => Hide();

        private void AddBackgroundFilterToggle()
        {
            if (_toggleMusicBtn == null)
            {
                return;
            }

            var buttonObject = Instantiate(_toggleMusicBtn.gameObject, _toggleMusicBtn.transform.parent);
            buttonObject.name = "BackgroundFilterToggle";
            _backgroundFilterLabel = buttonObject.GetComponentInChildren<TMP_Text>();
            UpdateBackgroundFilterLabel();

            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(CycleBackgroundFilter);
        }

        private void CycleBackgroundFilter()
        {
            var call = _streamVideoManager.ActiveCall;
            if (call == null)
            {
                return;
            }

            if (!call.IsBackgroundFilterSupported)
            {
                Debug.Log("Background filter is not supported on this device.");
                UpdateBackgroundFilterLabel();
                return;
            }

            var current = call.ActiveBackgroundFilter;
            if (current == null)
            {
                call.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Light));
            }
            else if (current.Intensity == BlurIntensity.Light)
            {
                call.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Medium));
            }
            else if (current.Intensity == BlurIntensity.Medium)
            {
                call.SetBackgroundFilter(BackgroundFilter.Blur(BlurIntensity.Heavy));
            }
            else
            {
                call.SetBackgroundFilter(null);
            }

            UpdateBackgroundFilterLabel();
        }

        private void UpdateBackgroundFilterLabel()
        {
            if (_backgroundFilterLabel == null)
            {
                return;
            }

            var call = _streamVideoManager.ActiveCall;
            if (call == null || !call.IsBackgroundFilterSupported)
            {
                _backgroundFilterLabel.text = "BG Blur: N/A";
                return;
            }

            var current = call.ActiveBackgroundFilter;
            _backgroundFilterLabel.text = current == null
                ? "BG Blur: Off"
                : "BG Blur: " + current.Intensity;
        }
    }
}
