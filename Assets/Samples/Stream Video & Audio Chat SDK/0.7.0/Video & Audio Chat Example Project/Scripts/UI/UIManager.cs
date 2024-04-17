using System;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Screens;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<WebCamTexture> ActiveCameraChanged;

        //public WebCamTexture ActiveCamera { get; private set; }
        //public AudioSource InputAudioSource => _inputAudioSource;
        public Camera InputSceneSource => _inputSceneCamera;

        public void ChangeMicrophone(MicrophoneDeviceInfo device, bool isActive)
        {
            var prevDevice = _videoManager.Client.AudioDeviceManager.SelectedDevice.Name ?? "None";
            _selectedMicrophoneDeviceName = device.Name;

            _videoManager.Client.AudioDeviceManager.SelectDevice(device);
            
            Debug.Log(
                $"Changed selected MICROPHONE from `{prevDevice}` to `{_selectedMicrophoneDeviceName}`. Recording: {isActive}");
        }

        public void ChangeCamera(CameraDeviceInfo device, bool isActive)
        {
            var cameraManager = _videoManager.Client.VideoDeviceManager;
            var prevDevice = cameraManager.SelectedDevice.Name ?? "None";
        
            cameraManager.SelectDevice(device);
        
            Debug.Log($"Changed active CAMERA from `{prevDevice}` to `{device}`");
        
            ActiveCameraChanged?.Invoke(cameraManager.GetSelectedDeviceWebCamTexture());
        }

        /// <summary>
        /// Start/stop microphone recording
        /// </summary>
        public void SetMicrophoneActive(bool isActive)
        {
            _videoManager.Client.AudioDeviceManager.SetEnabled(isActive);
        }

        /// <summary>
        /// Start/stop camera recording
        /// </summary>
        public void SetCameraActive(bool isActive)
        {
            _videoManager.Client.VideoDeviceManager.SetEnabled(isActive);
            
            if (isActive)
            {
                Debug.Log($"Camera recording started for `{_videoManager.Client.VideoDeviceManager.SelectedDevice.Name}`");
                return;
            }
            
            Debug.Log($"Camera recording stopped for `{_videoManager.Client.VideoDeviceManager.SelectedDevice.Name}`");
        }

        public void Log(string message, LogType type)
        {
            if (type == LogType.Exception)
            {
                throw new NotSupportedException("To log exceptions use " + nameof(Debug.LogException));
            }

            Debug.LogFormat(type, LogOption.None, context: null, format: message);
        }

        protected void Awake()
        {
            _videoManager.Init();
            
            _videoManager.CallStarted += OnCallStarted;
            _videoManager.CallEnded += OnCallEnded;

            _mainScreen.Init(_videoManager, uiManager: this);
            _callScreen.Init(_videoManager, uiManager: this);
        }

        protected void Start()
        {
            ShowMainScreen();
        }

        protected void OnDestroy()
        {
            _videoManager.CallStarted -= OnCallStarted;
            _videoManager.CallEnded -= OnCallEnded;
        }

        [SerializeField]
        private StreamVideoManager _videoManager;

        [SerializeField]
        private int _senderVideoWidth = 1280;

        [SerializeField]
        private int _senderVideoHeight = 720;

        [SerializeField]
        private int _senderVideoFps = 30;

        [SerializeField]
        private Camera _inputSceneCamera;

        [SerializeField]
        private CallScreenView _callScreen;

        [SerializeField]
        private MainScreenView _mainScreen;

        private string _selectedMicrophoneDeviceName;

        private void OnCallStarted(IStreamCall call) => ShowCallScreen(call);

        private void OnCallEnded() => ShowMainScreen();

        private void ShowMainScreen()
        {
            _callScreen.Hide();
            _mainScreen.Show();
        }

        private void ShowCallScreen(IStreamCall call)
        {
            _mainScreen.Hide();
            _callScreen.Show(new CallScreenView.ShowArgs(call));
        }
    }
}