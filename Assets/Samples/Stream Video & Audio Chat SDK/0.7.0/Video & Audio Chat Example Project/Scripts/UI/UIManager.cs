using System;
using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Screens;
using StreamVideo.Libs.Utils;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace StreamVideo.ExampleProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<WebCamTexture> LocalCameraChanged;

        public VideoResolution SenderVideoResolution => new VideoResolution(_senderVideoWidth, _senderVideoHeight);
        public int SenderVideoFps => _senderVideoFps;

        public void RequestCameraPermissions(Action onGranted = null, Action onDenied = null)
        {
#if UNITY_ANDROID
            var callbacks = new PermissionCallbacks();
            Permission.RequestUserPermission(Permission.Camera, callbacks);

            callbacks.PermissionGranted += _ => { onGranted?.Invoke(); };
            callbacks.PermissionDenied += permissionName =>
            {
                onDenied?.Invoke();
                Debug.LogError($"{permissionName} permission was not granted. Video capturing will not work.");
            };
            callbacks.PermissionDeniedAndDontAskAgain += (permissionName) =>
            {
                onDenied?.Invoke();
                Debug.LogError($"{permissionName} permission was not granted. Video capturing will not work.");
            };
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }

        public bool HasUserAuthorizedCameraPermission()
        {
#if UNITY_STANDALONE
            return true; //StreamTodo: check if this is true for all platforms    
#endif
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }

        public void RequestMicrophonePermissions(Action onGranted = null, Action onDenied = null)
        {
#if UNITY_ANDROID
            var callbacks = new PermissionCallbacks();

            Permission.RequestUserPermission(Permission.Microphone, callbacks);

            callbacks.PermissionGranted += _ => { onGranted?.Invoke(); };
            callbacks.PermissionDenied += permissionName =>
            {
                onDenied?.Invoke();
                Debug.LogError($"{permissionName} permission was not granted. Video capturing will not work.");
            };
            callbacks.PermissionDeniedAndDontAskAgain += (permissionName) =>
            {
                onDenied?.Invoke();
                Debug.LogError($"{permissionName} permission was not granted. Video capturing will not work.");
            };
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }

        public bool HasUserAuthorizedMicrophonePermission()
        {
#if UNITY_STANDALONE
            return true; //StreamTodo: check if this is true for all platforms    
#endif
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }

        protected void Awake()
        {
            _videoManager.Init();

            _videoManager.CallStarted += OnCallStarted;
            _videoManager.CallEnded += OnCallEnded;

            _videoManager.Client.VideoDeviceManager.SelectedDeviceChanged += OnCameraDeviceChanged;
            _videoManager.Client.AudioDeviceManager.SelectedDeviceChanged += OnMicrophoneDeviceChanged;

            _mainScreen.Init(_videoManager, uiManager: this);
            _callScreen.Init(_videoManager, uiManager: this);

            if (!HasUserAuthorizedCameraPermission())
            {
                RequestCameraPermissions(onGranted: () => { SelectFirstWorkingCameraOrDefaultAsync().LogIfFailed(); },
                    onDenied: ()
                        => Debug.LogError("Camera permission was not granted. Video capturing will not work."));
            }
            else
            {
                SelectFirstWorkingCameraOrDefaultAsync().LogIfFailed();
            }

            if (!HasUserAuthorizedMicrophonePermission())
            {
                RequestMicrophonePermissions(onGranted: SelectFirstMicrophone,
                    onDenied: ()
                        => Debug.LogError("Microphone permission was not granted. Audio capturing will not work."));
            }
            else
            {
                SelectFirstMicrophone();
            }
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
        private CallScreenView _callScreen;

        [SerializeField]
        private MainScreenView _mainScreen;

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

        private async Task SelectFirstWorkingCameraOrDefaultAsync()
        {
            if (!_videoManager.Client.VideoDeviceManager.EnumerateDevices().Any())
            {
                Debug.LogError(
                    "No camera devices found! Video streaming will not work. Please ensure that a camera device is plugged in.");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            foreach (var device in _videoManager.Client.VideoDeviceManager.EnumerateDevices())
            {
                if (!device.IsFrontFacing)
                {
                    continue;
                }

                var isWorking = await _videoManager.Client.VideoDeviceManager.TestDeviceAsync(device);
                if (isWorking)
                {
                    _videoManager.Client.VideoDeviceManager.SelectDevice(device, enable: false);
                    return;
                }
            }
#endif

            var workingDevice = await _videoManager.Client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
            if (workingDevice.HasValue)
            {
                _videoManager.Client.VideoDeviceManager.SelectDevice(workingDevice.Value, enable: false);
                return;
            }

            Debug.LogWarning("No working camera found. Falling back to first device.");

            var firstDevice = _videoManager.Client.VideoDeviceManager.EnumerateDevices().FirstOrDefault();
            if (firstDevice == default)
            {
                Debug.LogError(
                    "No camera devices found! Video streaming will not work. Please ensure that a camera device is plugged in.");
                return;
            }

            _videoManager.Client.VideoDeviceManager.SelectDevice(firstDevice, enable: false);
        }

        private void SelectFirstMicrophone()
        {
            // Select first microphone by default
            var microphoneDevice = _videoManager.Client.AudioDeviceManager.EnumerateDevices().FirstOrDefault();
            if (microphoneDevice == default)
            {
                Debug.LogError(
                    "No microphone devices found! Audio streaming will not work. Please ensure that a microphone device is plugged in.");
                return;
            }

            _videoManager.Client.AudioDeviceManager.SelectDevice(microphoneDevice, enable: false);
        }
    }
}