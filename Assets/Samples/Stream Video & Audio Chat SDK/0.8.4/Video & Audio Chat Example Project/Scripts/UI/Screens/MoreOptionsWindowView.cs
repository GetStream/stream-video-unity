using System;
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
            _toggleAudioModeBtn.onClick.AddListener(() => _streamVideoManager.ToggleAudioMode());
            _printAudioConfigBtn.onClick.AddListener(() => _streamVideoManager.PrintAudioConfig());
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
        private Button _toggleAudioModeBtn;
        
        [SerializeField]
        private Button _printAudioConfigBtn;

        [SerializeField]
        private Button _hideBtn;

        private StreamVideoManager _streamVideoManager;

        private void OnCloseButtonClicked() => Hide();
    }
}
