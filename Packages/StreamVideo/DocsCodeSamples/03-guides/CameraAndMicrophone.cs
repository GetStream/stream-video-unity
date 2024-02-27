using System.Linq;
using StreamVideo.Core;
using UnityEngine;

namespace DocsCodeSamples._03_guides
{
    internal class CameraAndMicrophone : MonoBehaviour
    {
        public void SetupMicrophoneInput()
        {
            // Obtain reference to an AudioSource that will be used a source of audio
            var inputAudioSource = GetComponent<AudioSource>();

// Get a valid microphone device name.
// You usually want to populate a dropdown list with Microphone.devices so that the user can pick which device should be used
            _activeMicrophoneDeviceName = Microphone.devices.First();

            inputAudioSource.clip
                = Microphone.Start(_activeMicrophoneDeviceName, true, 3, AudioSettings.outputSampleRate);
            inputAudioSource.loop = true;
            inputAudioSource.Play();

            _client.SetAudioInputSource(inputAudioSource);
        }

        public void ChangeMicrophoneDevice()
        {
            var newMicrophoneDeviceName = "test";
            
            // Stop previously active microphone
            Microphone.End(_activeMicrophoneDeviceName);

            // Obtain reference to an AudioSource that was setup as an input source
            var inputAudioSource = GetComponent<AudioSource>();

            inputAudioSource.clip = Microphone.Start(newMicrophoneDeviceName, true, 3, AudioSettings.outputSampleRate);
        }

        public void SetupCameraInput()
        {
            // Obtain a camera device
            var cameraDevice = WebCamTexture.devices.First();

            // Use device name to create a new WebCamTexture instance
            var activeCamera = new WebCamTexture(cameraDevice.name, 1920, 1080, 24);

            // Call Play() in order to start capturing the video
            activeCamera.Play();

            // Set WebCamTexture in Stream's Client - this WebCamTexture will be the video source in video calls
            _client.SetCameraInputSource(activeCamera);
        }

        public void ChangeVideoDevice()
        {
            // Item from WebCamTexture.devices
            var newDeviceName = "deviceName";
                
            _activeCamera.Stop();
            _activeCamera.deviceName = newDeviceName;
            _activeCamera.Play();
        }

        public void UpdateCameraInputSource()
        {
            // Obtain a camera device
            var cameraDevice = WebCamTexture.devices.First();

            // Use device name to create a new WebCamTexture instance
            var activeCamera = new WebCamTexture(cameraDevice.name);

            // Call Play() in order to start capturing the video
            activeCamera.Play();
            
            _client.SetCameraInputSource(activeCamera);
        }

        private IStreamVideoClient _client;
        private string _activeMicrophoneDeviceName;

        private WebCamTexture _activeCamera;
    }
}