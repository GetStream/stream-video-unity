using System;
using StreamVideo.ExampleProject.UI.Devices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    /// <summary>
    /// The main screen where a use can create a call or join
    /// </summary>
    public class MainScreenView : BaseScreenView<CallScreenView.ShowArgs>
    {
        /// <summary>
        /// Arguments required to initialize this screen when showing
        /// </summary>
        public readonly struct ShowArgs
        {
        }

        public void Show() => base.Show(new CallScreenView.ShowArgs());

        protected override void OnInit()
        {
            _joinBtn.onClick.AddListener(OnJoinCallButtonClicked);
            _createBtn.onClick.AddListener(OnCreateAndJoinCallButtonClicked);

            _audioRedToggle.onValueChanged.AddListener(VideoManager.SetAudioREDundancyEncoding);
            _audioDtxToggle.onValueChanged.AddListener(VideoManager.SetAudioDtx);
            
            _cameraPanel.Init(VideoManager.Client);
            _microphonePanel.Init(VideoManager.Client);
        }
        
        protected override void OnShow(CallScreenView.ShowArgs showArgs)
        {
            UIManager.LocalCameraChanged += OnLocalCameraChanged;
        }

        protected override void OnHide()
        {
            UIManager.LocalCameraChanged -= OnLocalCameraChanged;
        }

        [SerializeField]
        private Button _joinBtn;

        [SerializeField]
        private Button _createBtn;

        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        [SerializeField]
        private RawImage _localCameraImage;

        [SerializeField]
        private Toggle _audioRedToggle;

        [SerializeField]
        private Toggle _audioDtxToggle;

        [SerializeField]
        private CameraMediaDevicePanel _cameraPanel;

        [SerializeField]
        private MicrophoneMediaDevicePanel _microphonePanel;

        private WebCamDevice _defaultCamera;
        private string _defaultMicrophoneDeviceName;

        private async void OnJoinCallButtonClicked()
        {
            try
            {
                if (string.IsNullOrEmpty(_joinCallIdInput.text))
                {
                    Debug.LogError("`Call ID` is required when trying to join a call");
                    return;
                }

                await VideoManager.JoinAsync(_joinCallIdInput.text, create: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async void OnCreateAndJoinCallButtonClicked()
        {
            try
            {
                var callId = CreateRandomCallId();
                await VideoManager.JoinAsync(callId, create: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnLocalCameraChanged(WebCamTexture activeCamera)
        {
            _localCameraImage.texture = activeCamera;
        }

        private static string CreateRandomCallId() => Guid.NewGuid().ToString().Replace("-", "");
    }
}