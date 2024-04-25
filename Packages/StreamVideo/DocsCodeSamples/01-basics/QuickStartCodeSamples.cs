using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

namespace StreamVideoDocsCodeSamples._01_basics
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

        public void ListAvailableMicrophoneDevices()
        {
            var microphones = _client.AudioDeviceManager.EnumerateDevices();

            foreach (var mic in microphones)
            {
                Debug.Log(mic.Name);
            }
        }

        public void SelectAudioCapturingDevice()
        {
            // Enumerate available microphone devices
            var microphones = _client.AudioDeviceManager.EnumerateDevices();

            foreach (var mic in microphones)
            {
                Debug.Log(mic.Name);
            }

            var firstMicrophone = microphones.First();

            // Select microphone device to capture audio input. `enable` argument determines whether audio capturing should start
            _client.AudioDeviceManager.SelectDevice(firstMicrophone, enable: true);
        }

        public void StartStopAudioRecording()
        {
            // Start audio capturing
            _client.AudioDeviceManager.Enable();

            // Stop audio capturing
            _client.AudioDeviceManager.Disable();
        }

        public void RequestMicrophonePermissionsIOSandWebGL()
        {
            // Request microphone permissions
            Application.RequestUserAuthorization(UserAuthorization.Microphone);

            // Check if user granted microphone permission
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                // Notify user that microphone permission was not granted and the microphone capturing will not work.
            }
        }

        public void RequestMicrophonePermissionsAndroid()
        {
            // Request microphone permissions
            Permission.RequestUserPermission(Permission.Microphone);

            // Check if user granted microphone permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                // Notify user that microphone permission was not granted and the microphone capturing will not work.
            }
        }

        public void ListAvailableCameraDevices()
        {
            var cameras = _client.VideoDeviceManager.EnumerateDevices();

            foreach (var cam in cameras)
            {
                Debug.Log(cam.Name);
            }
        }

        public void SelectVideoCapturingDevice()
        {
            // Enumerate available camera devices
            var cameras = _client.VideoDeviceManager.EnumerateDevices();

            var firstCamera = cameras.First();

            // Select camera device to capture video input. `enable` argument determines whether video capturing should start
            _client.VideoDeviceManager.SelectDevice(firstCamera, enable: true);
        }

        public void StartStopVideoCapturing()
        {
            // Start video capturing
            _client.VideoDeviceManager.Enable();

            // Stop video capturing
            _client.VideoDeviceManager.Disable();
        }

        public void RequestCameraPermissionsIOSandWebGL()
        {
            // Request camera permissions
            Application.RequestUserAuthorization(UserAuthorization.WebCam);

            // Check if user granted camera permission
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                // Notify user that camera permission was not granted and the camera capturing will not work.
            }
        }

        public void RequestCameraPermissionsAndroid()
        {
            // Request camera permissions
            Permission.RequestUserPermission(Permission.Camera);

            // Check if user granted camera permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // Notify user that camera permission was not granted and the camera capturing will not work.
            }
        }

        private IStreamVideoClient _client;

        private string _activeMicrophoneDeviceName;
    }
}