using System;
using System.Linq;
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
            
            _cameraPanel.DeviceChanged += UIManager.ChangeCamera;
            _cameraPanel.DeviceToggled += UIManager.SetCameraActive;

            _microphonePanel.DeviceChanged += UIManager.ChangeMicrophone;
            _microphonePanel.DeviceToggled += UIManager.SetMicrophoneActive;
            
            SmartPickDefaultCamera();
            SmartPickDefaultMicrophone();
        }
        
        protected override void OnShow(CallScreenView.ShowArgs showArgs)
        {
            UIManager.ActiveCameraChanged += OnActiveCameraChanged;
        }

        protected override void OnHide()
        {
            UIManager.ActiveCameraChanged -= OnActiveCameraChanged;
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
                    Log("`Call ID` is required when trying to join a call", LogType.Error);
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

        private void OnActiveCameraChanged(WebCamTexture activeCamera)
        {
            _localCameraImage.texture = activeCamera;
        }

        private void SmartPickDefaultCamera()
        {
            var devices = WebCamTexture.devices;

#if UNITY_STANDALONE_WIN
            //StreamTodo: remove this, "Capture" is our debug camera
            _defaultCamera = devices.FirstOrDefault(d => d.name.Contains("Capture"));

#elif UNITY_ANDROID || UNITY_IOS
            _defaultCamera = devices.FirstOrDefault(d => d.isFrontFacing);
#endif

            if (string.IsNullOrEmpty(_defaultCamera.name))
            {
                _defaultCamera = devices.FirstOrDefault();
            }
            
            if (string.IsNullOrEmpty(_defaultCamera.name))
            {
                Debug.LogError("Failed to pick default camera device");
                return;
            }

            _cameraPanel.SelectDeviceWithoutNotify(_defaultCamera.name);
            UIManager.ChangeCamera(_defaultCamera.name, _cameraPanel.IsDeviceActive);
        }

        //StreamTodo: remove
        private void SmartPickDefaultMicrophone()
        {
            var preferredMicDevices = new[] { "bose", "airpods" };
            _defaultMicrophoneDeviceName = Microphone.devices.FirstOrDefault(d
                => preferredMicDevices.Any(m => d.IndexOf(m, StringComparison.OrdinalIgnoreCase) != -1));

            if (string.IsNullOrEmpty(_defaultMicrophoneDeviceName))
            {
                _defaultMicrophoneDeviceName = Microphone.devices.FirstOrDefault();
            }
            
            if (string.IsNullOrEmpty(_defaultMicrophoneDeviceName))
            {
                Debug.LogError("Failed to pick default microphone device");
                return;
            }

            _microphonePanel.SelectDeviceWithoutNotify(_defaultMicrophoneDeviceName);
            UIManager.ChangeMicrophone(_defaultMicrophoneDeviceName, _microphonePanel.IsDeviceActive);
        }

        private static string CreateRandomCallId() => Guid.NewGuid().ToString().Replace("-", "");
    }
}