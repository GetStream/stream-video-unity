using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;

namespace DocsCodeSamples._03_guides
{
    internal class CallAndParticipantState
    {
        public async void GetOrCreateCall()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);
        }
        
        public async void ParticipantState()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Notice that we pass create argument as true - this will create the call if it doesn't already exist
            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            foreach (var participant in streamCall.Participants)
            {
                // Handle call participant. For example: spawn a prefab that will contain RawImage to show the video and an AudioSource to play the audio

                // Iterate over participant tracks. They can be either of type `StreamVideoTrack` or `StreamAudioTrack`
                foreach (var track in participant.GetTracks())
                {
                    
                }
                
                // Subscribe to `TrackAdded` event in order to get notified about new tracks added later
                participant.TrackAdded += OnParticipantTrackAdded;
            }

            // Subscribe to `ParticipantJoined` and `ParticipantLeft` to get notified when a new participant joins the call or a participant left the call
            streamCall.ParticipantJoined += OnParticipantJoined; 
            streamCall.ParticipantLeft += OnParticipantLeft; 
        }

        private void OnParticipantLeft(string sessionid, string userid)
        {
        }

        private void OnParticipantJoined(IStreamVideoCallParticipant participant)
        {
        }

        private void OnParticipantTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
        {
        }

        private IStreamVideoClient _client;
    }
}