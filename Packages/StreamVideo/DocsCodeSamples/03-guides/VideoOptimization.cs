using System.Threading.Tasks;
using StreamVideo.Core;

namespace DocsCodeSamples._03_guides
{
    internal class VideoOptimization
    {
        public async Task ControlParticipantVideoResolution()
        {
            var callType = StreamCallType.Default; // Call type affects default permissions
            var callId = "my-call-id";

            // Notice that we pass create argument as true - this will create the call if it doesn't already exist
            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: false, notify: false);

            foreach (var participant in streamCall.Participants)
            {
                participant.UpdateRequestedVideoResolution(new VideoResolution(1280, 720));
            }
        }
        
        private IStreamVideoClient _client;
    }
}