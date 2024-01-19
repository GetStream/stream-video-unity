using System;
using StreamVideo.Core.StatefulModels;
using StreamVideo.ExampleProject.UI.Screens;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<WebCamTexture> ActiveCameraChanged;

        public WebCamTexture ActiveCamera { get; private set; }
        public AudioSource InputAudioSource => _inputAudioSource;
        public Camera InputSceneSource => _inputSceneCamera;

        public void ChangeMicrophone(string deviceName, bool isActive)
        {
            StopAudioRecording();
            var prevDevice = _activeMicrophoneDeviceName;
            _activeMicrophoneDeviceName = deviceName;
            Debug.Log($"Changed active MICROPHONE from `{prevDevice}` to `{_activeMicrophoneDeviceName}`");

            if (isActive)
            {
                StartAudioRecording();
            }
        }

        public void ChangeCamera(string deviceName)
        {
            var prevDevice = ActiveCamera != null ? ActiveCamera.deviceName : "None";

            if (ActiveCamera == null)
            {
                ActiveCamera = new WebCamTexture(deviceName, _senderVideoWidth, _senderVideoHeight, _senderVideoFps);
            }

            ActiveCamera.Stop();
            ActiveCamera.deviceName = deviceName;
            ActiveCamera.Play();

            Debug.Log($"Changed active CAMERA from `{prevDevice}` to `{deviceName}`");

            // If we're during the call let's update the camera input source
            var isCallActive = _videoManager.Client != null && _videoManager.Client.ActiveCall != null;
            if (isCallActive)
            {
                _videoManager.Client.SetCameraInputSource(ActiveCamera);
            }

            ActiveCameraChanged?.Invoke(ActiveCamera);
        }

        /// <summary>
        /// Use this to mute/unmute microphone
        /// </summary>
        /// <param name="enabled"></param>
        public void SetMicrophoneActive(bool enabled)
        {
            if (enabled)
            {
                StartAudioRecording();
                return;
            }

            StopAudioRecording();
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
            _videoManager.CallStarted += OnCallStarted;
            _videoManager.CallEnded += OnCallEnded;

            _mainScreen.Init(_videoManager, uiManager: this);
            _callScreen.Init(_videoManager, uiManager: this);
        }

        protected void Start() => ShowMainScreen();

        protected void OnDestroy()
        {
            _videoManager.CallStarted -= OnCallStarted;
            _videoManager.CallEnded -= OnCallEnded;
        }

        [SerializeField]
        private StreamVideoManager _videoManager;

        [Header("Video Sending Settings")]
        [SerializeField]
        private int _senderVideoWidth = 1280;

        [SerializeField]
        private int _senderVideoHeight = 720;

        [SerializeField]
        private int _senderVideoFps = 30;

        [Header("Input Sources")]
        [SerializeField]
        private AudioSource _inputAudioSource;

        [SerializeField]
        private Camera _inputSceneCamera;

        [Header("Screen Views")]
        [SerializeField]
        private CallScreenView _callScreen;

        [SerializeField]
        private MainScreenView _mainScreen;

        private string _activeMicrophoneDeviceName;

        private void OnCallStarted(IStreamCall call) => ShowCallScreen(call);

        private void OnCallEnded() => ShowMainScreen();

        private void StartAudioRecording()
        {
            if (_inputAudioSource == null)
            {
                Debug.LogError("Input Audio Source is null");
                return;
            }

            //StreamTodo: should the volume be 0 so we never hear input from our own microphone?
            _inputAudioSource.clip
                = Microphone.Start(_activeMicrophoneDeviceName, true, 3, AudioSettings.outputSampleRate);
            _inputAudioSource.loop = true;
            _inputAudioSource.Play();

            Debug.Log("Audio recording started. Device name: " + _activeMicrophoneDeviceName);
        }

        private void StopAudioRecording()
        {
            Microphone.End(_activeMicrophoneDeviceName);
            Debug.Log("Audio recording stopped");
        }

        private void ShowMainScreen()
        {
            _callScreen.Hide();
            _mainScreen.Show();
        }

        private void ShowCallScreen(IStreamCall call)
        {
            _mainScreen.Hide();
            _callScreen.Show(new CallScreenView.InitArgs(call));
        }
    }
}