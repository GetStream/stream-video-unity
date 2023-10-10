using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.UI;

namespace DocsCodeSamples._01_basics
{
    internal class QuickStartCodeSamples : MonoBehaviour
    {
        public async void GetCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Try to get call - will return null if the call doesn't exist
            var streamCall = await _client.GetCallAsync(callType, callId);
        }

        public async void GetOrCreateCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);
        }

        public async void CreateCallAndJoin()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

// Notice that we pass create argument as true - this will create the call if it doesn't already exist
            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: true, notify: false);
        }

        public async void JoinOtherCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

// Notice that we pass create argument as false - if the call doesn't exist the join attempt will fail
            var streamCall = await _client.JoinCallAsync(callType, callId, create: false, ring: true, notify: false);
        }

        public async void GetCallParticipants()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";
            
            var streamCall = await _client.JoinCallAsync(callType, callId, create: false, ring: true, notify: false);
            
            // Subscribe to events to get notified that streamCall.Participants collection changed
            streamCall.ParticipantJoined += OnParticipantJoined;
            streamCall.ParticipantLeft += OnParticipantLeft;
            
            // Iterate through current participants
            foreach (var participant in streamCall.Participants)
            {
                // Handle participant logic. For example: create a view for each participant
            }
        }

        private void OnParticipantLeft(string sessionid, string userid)
        {
        }

        private void OnParticipantJoined(IStreamVideoCallParticipant participant)
        {
        }

        public async Task HandleTracks()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // JoinCall to get a IStreamCall object. You can also call _client.GetOrCreateCallAsync or _client.GetCallAsync
            var streamCall = await _client.JoinCallAsync(callType, callId, create: false, ring: true, notify: false);

            // Subscribe for participants change
            streamCall.ParticipantJoined += OnParticipantJoined;
            streamCall.ParticipantLeft += OnParticipantLeft;

            // Process current participant
            foreach (var participant in streamCall.Participants)
            {
                // Handle currently available tracks
                foreach (var track in participant.GetTracks())
                {
                    OnParticipantTrackAdded(participant, track);
                }

                // Subscribe to event in case new tracks are added
                participant.TrackAdded += OnParticipantTrackAdded;
            }
        }

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
            switch (track)
            {
                case StreamAudioTrack streamAudioTrack:

                    // This assumes that this gameObject contains the AudioSource component but it's not a requirement. You can obtain the AudioSource reference in your preferred way
                    var audioSource = GetComponent<AudioSource>();
                        
                    // This AudioSource will receive audio from the participant
                    streamAudioTrack.SetAudioSourceTarget(audioSource);
                    break;

                case StreamVideoTrack streamVideoTrack:
                    
                    // This assumes that this gameObject contains the RawImage component but it's not a requirement. You can obtain the RawImage reference in your preferred way
                    var rawImage = GetComponent<RawImage>();
                        
                    // This RawImage will receive video from the participant
                    streamVideoTrack.SetRenderTarget(rawImage);
                    break;
            }
        }

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