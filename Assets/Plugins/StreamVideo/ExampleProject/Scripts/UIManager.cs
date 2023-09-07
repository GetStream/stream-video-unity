using System;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public delegate void JoinCallHandler(bool create);

    public class UIManager : MonoBehaviour
    {
        public event JoinCallHandler JoinClicked;
        public event Action CameraInputChanged;

        public AudioSource InputAudioSource => _inputAudioSource;
        public WebCamTexture InputCameraSource => _activeCamera;
        public Camera InputSceneCamera => _inputSceneCamera;
        public string JoinCallId => _joinCallIdInput.text;

        public int Width = 1280;
        public int Height = 720;
        public int FPS = 30;

        public void AddParticipant(IStreamVideoCallParticipant participant)
        {
            var view = Instantiate(_participantViewPrefab, _participantsContainer);
            view.Init(participant);
            _participantSessionIdToView.Add(participant.SessionId, view);
        }

        public void RemoveParticipant(string sessionId, string userId)
        {
            if (!_participantSessionIdToView.TryGetValue(sessionId, out var view))
            {
                Debug.LogError("Failed to find view for removed participant with sessionId: " + sessionId);
                return;
            }

            _participantSessionIdToView.Remove(sessionId);
            Destroy(view.gameObject);
        }

        public void SetJoinCallId(string joinCallId) => _joinCallIdInput.text = joinCallId;

        protected void Awake()
        {
            _joinBtn.onClick.AddListener(() => JoinClicked?.Invoke(false));
            _createBtn.onClick.AddListener(() => JoinClicked?.Invoke(true));

            _microphoneDeviceDropdown.ClearOptions();
            _microphoneDeviceDropdown.onValueChanged.AddListener(OnMicrophoneDeviceChanged);
            _microphoneDeviceDropdown.AddOptions(Microphone.devices.ToList());

            _cameraDeviceDropdown.ClearOptions();
            _cameraDeviceDropdown.onValueChanged.AddListener(OnCameraChanged);
            var cameraDevices = WebCamTexture.devices;

            foreach (var device in cameraDevices)
            {
                _cameraDeviceDropdown.options.Add(new TMP_Dropdown.OptionData(device.name));
            }

            SmartPickDefaultCamera();
            SmartPickDefaultMicrophone();
        }

        protected void Start()
        {
            _microphoneDeviceToggle.onValueChanged.AddListener(OnMicrophoneToggled);
            OnMicrophoneToggled(_microphoneDeviceToggle.enabled);

            //StreamTodo: handle camera toggle
        }

        private readonly Dictionary<string, ParticipantView> _participantSessionIdToView
            = new Dictionary<string, ParticipantView>();

        [SerializeField]
        private Button _joinBtn;

        [SerializeField]
        private Button _createBtn;

        [SerializeField]
        private TMP_InputField _joinCallIdInput;

        [SerializeField]
        private Transform _participantsContainer;

        [SerializeField]
        private ParticipantView _participantViewPrefab;

        [SerializeField]
        private TMP_Dropdown _microphoneDeviceDropdown;

        [SerializeField]
        private Toggle _microphoneDeviceToggle;

        [SerializeField]
        private TMP_Dropdown _cameraDeviceDropdown;

        [SerializeField]
        private Toggle _cameraDeviceToggle;

        [SerializeField]
        private AudioSource _inputAudioSource;

        [SerializeField]
        private RawImage _localCameraImage;
        
        [SerializeField]
        private Camera _inputSceneCamera;

        private string _activeMicrophoneDeviceName;

        private WebCamTexture _activeCamera;
        private WebCamDevice _defaultCamera;

        private void OnMicrophoneDeviceChanged(int index)
        {
            StopAudioRecording();
            _activeMicrophoneDeviceName = _microphoneDeviceDropdown.options[index].text;
            Debug.Log("Microphone device changed to: " + _activeMicrophoneDeviceName);

            if (_microphoneDeviceToggle.enabled)
            {
                StartAudioRecording();
            }
        }

        private void OnMicrophoneToggled(bool enabled)
        {
            if (enabled)
            {
                StartAudioRecording();
                return;
            }

            StopAudioRecording();
        }

        //StreamTodo: move to some media manager
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

        private void SmartPickDefaultCamera()
        {
            var devices = WebCamTexture.devices;

#if UNITY_STANDALONE_WIN

            //StreamTodo: remove this, "Capture" is our debug camera
            _defaultCamera = devices.FirstOrDefault(d => d.name.Contains("Capture"));

#elif UNITY_ANDROID || UNITY_IOS
        _defaultCamera = devices.FirstOrDefault(d => d.isFrontFacing);

#else
        _defaultCamera = devices.FirstOrDefault();

#endif

            if (string.IsNullOrEmpty(_defaultCamera.name))
            {
                Debug.LogError("Failed to pick default camera device");
                return;
            }

            Debug.Log($"---------- Default Camera: {_defaultCamera.name}");

            if (!string.IsNullOrEmpty(_defaultCamera.name))
            {
                ChangeCamera(_defaultCamera.name);
            }
        }

        //StreamTodo: remove
        private void SmartPickDefaultMicrophone()
        {
            var preferredMicDevices = new[] { "bose", "airpods" };
            var defaultMicrophone = Microphone.devices.FirstOrDefault(d
                => preferredMicDevices.Any(m => d.IndexOf(m, StringComparison.OrdinalIgnoreCase) != -1));

            if (!string.IsNullOrEmpty(defaultMicrophone))
            {
                var index = Array.IndexOf(Microphone.devices, defaultMicrophone);
                if (index == -1)
                {
                    Debug.LogError("Failed to find index of smart picked microphone");
                    return;
                }

                _microphoneDeviceDropdown.value = index;
            }
        }

        private void OnCameraChanged(int optionIndex) => ChangeCamera(_cameraDeviceDropdown.options[optionIndex].text);

        private void ChangeCamera(string deviceName)
        {
            if (_activeCamera != null && _activeCamera.isPlaying)
            {
                _activeCamera.Stop();
            }

            _activeCamera = new WebCamTexture(deviceName, Width, Height, FPS);

            _localCameraImage.texture = _activeCamera;

            _activeCamera.Play();

            CameraInputChanged?.Invoke();
        }
    }
}