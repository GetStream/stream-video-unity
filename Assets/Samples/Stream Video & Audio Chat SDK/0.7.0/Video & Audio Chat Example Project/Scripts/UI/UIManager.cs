using System;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Screens;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<WebCamTexture> LocalCameraChanged;

        public Camera InputSceneSource => _inputSceneCamera;
        
        public VideoResolution SenderVideoResolution => new VideoResolution(_senderVideoWidth, _senderVideoHeight);
        public int SenderVideoFps => _senderVideoFps;

        protected void Awake()
        {
            _videoManager.Init();

            _videoManager.CallStarted += OnCallStarted;
            _videoManager.CallEnded += OnCallEnded;

            _videoManager.Client.VideoDeviceManager.SelectedDeviceChanged += OnCameraDeviceChanged;
            _videoManager.Client.AudioDeviceManager.SelectedDeviceChanged += OnMicrophoneDeviceChanged;

            _mainScreen.Init(_videoManager, uiManager: this);
            _callScreen.Init(_videoManager, uiManager: this);
            
            TrySelectFirstWorkingCameraAsync().LogIfFailed();
            TrySelectFirstMicrophoneAsync().LogIfFailed();
        }

        protected void Start() => ShowMainScreen();

        protected void OnDestroy()
        {
            _videoManager.CallStarted -= OnCallStarted;
            _videoManager.CallEnded -= OnCallEnded;

            if (_videoManager.Client != null)
            {
                _videoManager.Client.VideoDeviceManager.SelectedDeviceChanged -= OnCameraDeviceChanged;
                _videoManager.Client.AudioDeviceManager.SelectedDeviceChanged -= OnMicrophoneDeviceChanged;
            }
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
        
        private void OnMicrophoneDeviceChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice)
        {
            Debug.Log($"Changed selected MICROPHONE from `{previousDevice}` to `{currentDevice}`");
        }

        private void OnCameraDeviceChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
        {
            Debug.Log($"Changed active CAMERA from `{previousDevice}` to `{currentDevice}`");

            var webCamTexture = _videoManager.Client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();
            LocalCameraChanged?.Invoke(webCamTexture);
        }
        
        private async Task TrySelectFirstWorkingCameraAsync()
        {
            var workingDevice = await _videoManager.Client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
            if (!workingDevice.HasValue)
            {
                Debug.LogError("No working camera found");
                return;
            }

            _videoManager.Client.VideoDeviceManager.SelectDevice(workingDevice.Value);
        }

        private Task TrySelectFirstMicrophoneAsync()
        {
            // Select first microphone by default
            var microphoneDevice = _videoManager.Client.AudioDeviceManager.EnumerateDevices().FirstOrDefault();
            if (microphoneDevice == default)
            {
                Debug.LogError("No microphone found");
                return Task.CompletedTask;
            }
            
            _videoManager.Client.AudioDeviceManager.SelectDevice(microphoneDevice);
            return Task.CompletedTask;
        }
    }
}