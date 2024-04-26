using System.Linq;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using UnityEngine;
using UnityEngine.Android;

namespace StreamVideoDocsCodeSamples._03_guides
{
    internal class CameraAndMicrophone_Microphone
    {
        public void ListAvailableMicrophoneDevices()
        {
            var microphones = _client.AudioDeviceManager.EnumerateDevices();

            foreach (var mic in microphones)
            {
                Debug.Log(mic.Name); // Get microphone name
            }
        }

        public void SelectMicrophone()
        {
            // Get available microphone devices. Returns IEnumerable<MicrophoneDeviceInfo>
            var microphones = _client.AudioDeviceManager.EnumerateDevices();

            foreach (var mic in microphones)
            {
                Debug.Log(mic.Name); // Get the name of the microphone
            }

            var microphone = microphones.First();

            // Select device for audio capturing. Pass the `enable` argument to control if capturing should be enabled
            _client.AudioDeviceManager.SelectDevice(microphone, enable: true);
        }

        public void GetSelectedMicrophone()
        {
            var selectedMicrophone = _client.AudioDeviceManager.SelectedDevice;
        }

        public void StartStopMicrophone()
        {
            // Enable device to start capturing microphone input
            _client.AudioDeviceManager.Enable();

            // Disable device to stop capturing microphone input
            _client.AudioDeviceManager.Disable();

            // Set the enabled state by passing a boolean argument
            _client.AudioDeviceManager.SetEnabled(true);
        }

        public void CheckMicrophoneStatus()
        {
            // Check if currently selected device is enabled
            var isDeviceEnabled = _client.AudioDeviceManager.IsEnabled;
        }

        public void AudioDeviceManagerEvents()
        {
            // Triggered when the selected devices changes
            _client.AudioDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;

            // Triggered when the IsEnabled property changes
            _client.AudioDeviceManager.IsEnabledChanged += OnIsEnabledChanged;
        }

        private void OnIsEnabledChanged(bool isEnabled)
        {
        }

        private void OnSelectedDeviceChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice)
        {
        }

        public void MicrophoneTesting()
        {
            var microphones = _client.AudioDeviceManager.EnumerateDevices();
            var microphone = microphones.First();

            // Testing devices

            _client.AudioDeviceManager.TestDeviceAsync(microphone);

            _client.AudioDeviceManager.TryFindFirstWorkingDeviceAsync();
        }

        public void MicrophoneIOSPermissions()
        {
            // Request permission to use the Microphone
            Application.RequestUserAuthorization(UserAuthorization.Microphone);

            // Check if user granted microphone permission
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                // Notify user that microphone permission was not granted and the microphone capturing will not work.
            }
        }

        public void MicrophoneAndroidPermissions()
        {
            // Request microphone permissions
            Permission.RequestUserPermission(Permission.Microphone);

            // Check if user granted microphone permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                // Notify user that microphone permission was not granted and the microphone capturing will not work.
            }
        }
        
        private IStreamVideoClient _client;
    }
}