using System.Linq;
using StreamVideo.Core;
using UnityEngine;

namespace DocsCodeSamples._01_basics
{
    internal class QuickStartCodeSamples : MonoBehaviour
    {
        public void SetAudioInput()
        {
            // Obtain reference to an AudioSource that will be used a source of audio
            var audioSource = GetComponent<AudioSource>();
            _client.SetAudioInputSource(audioSource);
        }

        public void BindMicrophoneToAudioSource()
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
        }

        public void StopAudioRecording()
        {
            Microphone.End(_activeMicrophoneDeviceName);
        }
        
        public void SetVideoInput()
        {
// Obtain reference to a WebCamTexture that will be used a source of video
var webCamTexture = GetComponent<WebCamTexture>();
_client.SetCameraInputSource(webCamTexture);
        }

        public void BindCameraToWebCamTexture()
        {
// Obtain a camera device
var cameraDevice = WebCamTexture.devices.First();

var width = 1920;
var height = 1080;
var fps = 30;

// Use device name to create a new WebCamTexture instance
var activeCamera = new WebCamTexture(cameraDevice.name, width, height, fps);

// Call Play() in order to start capturing the video
activeCamera.Play();

// Set WebCamTexture in Stream's Client - this WebCamTexture will be the video source in video calls
_client.SetCameraInputSource(activeCamera);
        }
        
        private IStreamVideoClient _client;

        private string _activeMicrophoneDeviceName;
    }
}