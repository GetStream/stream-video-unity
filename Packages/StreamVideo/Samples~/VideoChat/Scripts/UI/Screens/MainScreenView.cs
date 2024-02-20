using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.ExampleProject.UI.Screens
{
    /// <summary>
    /// The main screen where a use can create a call or join
    /// </summary>
    public class MainScreenView : BaseScreenView<CallScreenView.InitArgs>
    {
        /// <summary>
        /// Arguments required to initialize this screen when showing
        /// </summary>
        public readonly struct InitArgs
        {
        }

        public void Show() => base.Show(new CallScreenView.InitArgs());

        protected override void OnShow(CallScreenView.InitArgs initArgs)
        {
            _joinBtn.onClick.AddListener(OnJoinCallButtonClicked);
            _createBtn.onClick.AddListener(OnCreateAndJoinCallButtonClicked);

            _audioRedToggle.onValueChanged.AddListener(VideoManager.SetAudioREDundancyEncoding);
            _audioDtxToggle.onValueChanged.AddListener(VideoManager.SetAudioDtx);

            _microphoneDeviceDropdown.ClearOptions();
            _microphoneDeviceDropdown.onValueChanged.AddListener(OnMicrophoneDeviceChanged);
            _microphoneDeviceDropdown.AddOptions(Microphone.devices.ToList());

            _microphoneDeviceToggle.onValueChanged.AddListener(UIManager.SetMicrophoneActive);

            _cameraDeviceDropdown.ClearOptions();
            _cameraDeviceDropdown.onValueChanged.AddListener(OnCameraDeviceChanged);
            var cameraDevices = WebCamTexture.devices;

            foreach (var device in cameraDevices)
            {
                _cameraDeviceDropdown.options.Add(new TMP_Dropdown.OptionData(device.name));
            }

            SmartPickDefaultCamera();
            SmartPickDefaultMicrophone();

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
        private TMP_Dropdown _microphoneDeviceDropdown;

        [SerializeField]
        private Toggle _microphoneDeviceToggle;

        [SerializeField]
        private TMP_Dropdown _cameraDeviceDropdown;

        [SerializeField]
        private Toggle _cameraDeviceToggle;

        [SerializeField]
        private RawImage _localCameraImage;

        [SerializeField]
        private Toggle _audioRedToggle;

        [SerializeField]
        private Toggle _audioDtxToggle;

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

                // Set input sources before connecting with other participants
                SetInputSources();

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
                // Set input sources before connecting with other participants
                SetInputSources();

                var callId = CreateRandomCallId();
                await VideoManager.JoinAsync(callId, create: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SetInputSources()
        {
            VideoManager.Client.SetAudioInputSource(UIManager.InputAudioSource);
            VideoManager.Client.SetCameraInputSource(UIManager.ActiveCamera);

            // Optional, only needed when you want to stream scene Camera render
            VideoManager.Client.SetCameraInputSource(UIManager.InputSceneSource);
        }

        private void OnCameraDeviceChanged(int optionIndex)
            => UIManager.ChangeCamera(_cameraDeviceDropdown.options[optionIndex].text);

        private void OnMicrophoneDeviceChanged(int index)
            => UIManager.ChangeMicrophone(_microphoneDeviceDropdown.options[index].text,
                _microphoneDeviceToggle.enabled);

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
#else
            _defaultCamera = devices.FirstOrDefault();
#endif

            if (string.IsNullOrEmpty(_defaultCamera.name))
            {
                Debug.LogWarning("Failed to pick default camera device");
                return;
            }

            SetCameraDropdown(_defaultCamera.name);
        }

        //StreamTodo: remove
        private void SmartPickDefaultMicrophone()
        {
            var preferredMicDevices = new[] { "bose", "airpods" };
            _defaultMicrophoneDeviceName = Microphone.devices.FirstOrDefault(d
                => preferredMicDevices.Any(m => d.IndexOf(m, StringComparison.OrdinalIgnoreCase) != -1));

            if (string.IsNullOrEmpty(_defaultMicrophoneDeviceName))
            {
                Debug.LogWarning("Failed to pick default microphone device");
                return;
            }

            SetMicrophoneDropdown(_defaultMicrophoneDeviceName);
        }

        private void SetMicrophoneDropdown(string deviceName)
        {
            var index = Array.IndexOf(Microphone.devices, deviceName);
            if (index == -1)
            {
                Debug.LogError($"Failed to find index for microphone device: {deviceName}");
                return;
            }

            _microphoneDeviceDropdown.value = index;
        }
        
        private void SetCameraDropdown(string deviceName)
        {
            var index = Array.IndexOf(WebCamTexture.devices.Select(d => d.name).ToArray(), deviceName);
            if (index == -1)
            {
                Debug.LogError($"Failed to find index for camera device: {deviceName}");
                return;
            }

            _cameraDeviceDropdown.value = index;
        }

        private static string CreateRandomCallId() => Guid.NewGuid().ToString().Replace("-", "");
    }
}