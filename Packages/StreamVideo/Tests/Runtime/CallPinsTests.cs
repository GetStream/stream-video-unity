using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    internal class CallPinsTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_participant_pinned_expect_pinned_participants_changed_event_fired()
            => ConnectAndExecute(When_participant_pinned_expect_pinned_participants_changed_event_fired_Async);

        private async Task When_participant_pinned_expect_pinned_participants_changed_event_fired_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            var participant = streamCall.Participants.First();

            var eventWasFired = false;
            streamCall.PinnedParticipantsUpdated += () => eventWasFired = true;

            streamCall.PinLocally(participant);

            Assert.IsTrue(eventWasFired);
        }
        
        [UnityTest]
        public IEnumerator When_participant_unpinned_expect_pinned_participants_changed_event_fired()
            => ConnectAndExecute(When_participant_unpinned_expect_pinned_participants_changed_event_fired_Async);

        private async Task When_participant_unpinned_expect_pinned_participants_changed_event_fired_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            var participant = streamCall.Participants.First();

            streamCall.PinLocally(participant);
            
            var eventWasFired = false;
            streamCall.PinnedParticipantsUpdated += () => eventWasFired = true;
            
            streamCall.UnpinLocally(participant);

            Assert.IsTrue(eventWasFired);
        }
        
        [UnityTest]
        public IEnumerator When_participant_pinned_expect_pinned_participants_collection_contains()
            => ConnectAndExecute(When_participant_pinned_expect_pinned_participants_collection_contains_Async);

        private async Task When_participant_pinned_expect_pinned_participants_collection_contains_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            var participant = streamCall.Participants.First();

            streamCall.PinLocally(participant);

            var contains = streamCall.PinnedParticipants.Contains(participant);
            Assert.IsTrue(contains);
        }
        
        [UnityTest]
        public IEnumerator When_participant_unpinned_expect_pinned_participants_collection_updated()
            => ConnectAndExecute(When_participant_unpinned_expect_pinned_participants_collection_updated_Async);

        private async Task When_participant_unpinned_expect_pinned_participants_collection_updated_Async()
        {
            var streamCall = await JoinRandomCallAsync();
            var participant = streamCall.Participants.First();

            streamCall.PinLocally(participant);
            streamCall.UnpinLocally(participant);

            var contains = streamCall.PinnedParticipants.Contains(participant);
            Assert.IsFalse(contains);
        }
        
        //StreamTodo: when participant leaves expect him removed from PinnedParticipants
    }
}