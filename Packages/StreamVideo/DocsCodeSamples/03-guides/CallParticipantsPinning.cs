using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;

namespace DocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/pinning-participants/ page
    /// </summary>
    internal class CallParticipantsPinning
    {
        public async Task PinLocally()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            var participant = streamCall.Participants.First();
            
            streamCall.PinLocally(participant);
        }
        
        public async Task UnpinLocally()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            var participant = streamCall.Participants.First();
            
            streamCall.UnpinLocally(participant);
        }

        public async Task GetPinnedParticipants()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            var participant = streamCall.Participants.First();
            
            streamCall.PinLocally(participant);

            foreach (var pinnedParticipant in streamCall.PinnedParticipants)
            {
                // Iterate through pinned participants
            }
        }
        
        public async Task CheckIfParticipantIsPinned()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Get call or create if it doesn't exist
            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            var participant = streamCall.Participants.First();
            
            streamCall.PinLocally(participant);

            var isPinned = streamCall.IsPinnedLocally(participant);
            var isPinnedLocally = streamCall.IsPinnedLocally(participant);
            var isPinnedRemotely = streamCall.IsPinnedRemotely(participant);
        }
        
        private IStreamVideoClient _client;
    }
}