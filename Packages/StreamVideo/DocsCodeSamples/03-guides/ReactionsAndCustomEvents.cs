using System.Collections.Generic;
using System.Numerics;
using StreamVideo.Core;

namespace StreamVideoDocsCodeSamples._03_guides
{
    /// <summary>
    /// Code examples for guides/reactions-and-custom-events/ page
    /// </summary>
    internal class ReactionsAndCustomEvents
    {
        public async void SendReaction()
        {
            var callType = StreamCallType.Default;
            var callId = "my-call-id";

            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            await streamCall.SendReactionAsync("like");
        }

        public async void SendReactionAdvanced()
        {
            var callType = StreamCallType.Default;
            var callId = "my-call-id";

            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            await streamCall.SendReactionAsync(type: "like", emojiCode: ":like:",
                customData: new Dictionary<string, object>()
                {
                    // Put any parameters you want
                    { "position", new Vector2(100, 200) }
                });
        }

        public async void SendEvent()
        {
            var callType = StreamCallType.Default;
            var callId = "my-call-id";

            var streamCall = await _client.GetOrCreateCallAsync(callType, callId);

            await streamCall.SendCustomEventAsync(new Dictionary<string, object>
            {
                // Put any parameters you want
                { "type", "epic_event" },
                { "some_number", 50 },
                { "tags", new string[] { "ninja", "cat" } }
            });
        }

        private IStreamVideoClient _client;
    }
}