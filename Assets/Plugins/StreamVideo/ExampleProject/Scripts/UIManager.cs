using System;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject
{
    public class UIManager : MonoBehaviour
    {
        public event Action JoinClicked;
        
        public int Width = 1280;
        public int Height = 720;
        public int FPS = 30;

        public void AddParticipant(IStreamVideoCallParticipant participant)
        {
            var view = Instantiate(_participantViewPrefab, _participantsContainer);
            view.Init(participant);
        }

        protected void Awake()
        {
            _joinBtn.onClick.AddListener(() => JoinClicked?.Invoke());

            _microphoneDeviceDropdown.ClearOptions();
            _microphoneDeviceDropdown.onValueChanged.AddListener(OnMicrophoneDeviceChanged);
            _microphoneDeviceDropdown.AddOptions(Microphone.devices.ToList());

            _cameraDeviceDropdown.ClearOptions();
            _cameraDeviceDropdown.onValueChanged.AddListener(OnCameraChanged);
            var cameraDevices = WebCamTexture.devices;

            foreach (var device in cameraDevices)
            {
                _cameraDeviceDropdown.options.Add(new TMP_Dropdown.OptionData(device.name));

                var res = "None";
                if (device.availableResolutions != null)
                {
                    res = string.Join(",",
                        device.availableResolutions.Select(res
                            => $"[{res.width}:{res.height}]"));
                }

                Debug.Log(
                    $"-------- DEVICE: {device.name}, kind:{device.kind}, isFrontFacing:{device.isFrontFacing}, isAutoFocusPointSupported:{device.isAutoFocusPointSupported}, resolutions: {res}");
            }

            SmartPickDefaultCamera();
        }

        protected void Start()
        {
            _microphoneDeviceToggle.onValueChanged.AddListener(OnMicrophoneToggled);
            OnMicrophoneToggled(_microphoneDeviceToggle.enabled);
        }

        [SerializeField]
        private Button _joinBtn;

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

        private string _activeMicrophoneDeviceName;

        private WebCamTexture _activeCamera;
        private WebCamDevice _defaultDevice;

        private void OnMicrophoneDeviceChanged(int index)
        {
            StopAudioRecording();
            _activeMicrophoneDeviceName = _microphoneDeviceDropdown.options[index].text;
            Debug.Log("Microphone device changed to: " + _activeMicrophoneDeviceName);
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

            _defaultDevice = devices.FirstOrDefault(d => d.name.Contains("Capture"));

#elif UNITY_ANDROID || UNITY_IOS
        _defaultDevice = devices.FirstOrDefault(d => d.isFrontFacing);

#else
        _defaultDevice = devices.FirstOrDefault();

#endif

            if (string.IsNullOrEmpty(_defaultDevice.name))
            {
                Debug.LogError("Failed to pick default camera device");
                return;
            }

            Debug.Log($"---------- Default Camera: {_defaultDevice.name}");
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
        }
    }
}