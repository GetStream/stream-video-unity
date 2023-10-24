using StreamVideo.Core;

namespace DocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/call-types/ page
    /// </summary>
    internal class CallTypes
    {
        public async void GetOrCreateCall()
        {
            var callType = StreamCallType.Default;
            var callId = "my-call-id";

            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);
        }

        public async void CreateCallAndJoin()
        {
            var callType = StreamCallType.Default;
            var callId = "my-call-id";

            var streamCall = await _client.JoinCallAsync(callType, callId, create: true, ring: true, notify: false);
        }
        
        private IStreamVideoClient _client;
    }
}