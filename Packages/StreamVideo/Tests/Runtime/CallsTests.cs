#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    internal class CallsTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_two_clients_join_same_call_expect_no_errors()
            => ConnectAndExecute(When_two_clients_join_same_call_expect_no_errors_Async);

        private async Task When_two_clients_join_same_call_expect_no_errors_Async(ITestClient client,
            ITestClient client2)
        {
            var streamCall = await client.JoinRandomCallAsync();

            var call = await client2.Client.JoinCallAsync(streamCall.Type, streamCall.Id, create: false, ring: false,
                notify: false);

            Assert.AreEqual(2, call.Participants.Count);
        }

        [UnityTest]
        public IEnumerator When_client_joins_call_with_video_expect_receiving_video_track()
            => ConnectAndExecute(When_client_joins_call_with_video_expect_receiving_video_track_Async,
                ignoreFailingMessages: true);

        private async Task When_client_joins_call_with_video_expect_receiving_video_track_Async(
            ITestClient clientA, ITestClient clientB)
        {
            var streamCall = await clientA.JoinRandomCallAsync();

            var cameraDevice = await TestUtils.TryGetFirstWorkingCameraDeviceAsync(clientA.Client);
            Debug.Log("Selected camera device: " + cameraDevice);
            clientA.Client.VideoDeviceManager.SelectDevice(cameraDevice, enable: true);

            var call = await clientB.Client.JoinCallAsync(streamCall.Type, streamCall.Id, create: false,
                ring: false,
                notify: false);

            var otherParticipant = call.Participants.First(p => !p.IsLocalParticipant);

            StreamVideoTrack streamTrack = null;

            if (otherParticipant.VideoTrack != null)
            {
                streamTrack = (StreamVideoTrack)otherParticipant.VideoTrack;
            }
            else
            {
                otherParticipant.TrackAdded += (_, track) => { streamTrack = (StreamVideoTrack)track; };

                await WaitForConditionAsync(() => streamTrack != null);
            }

            Assert.IsNotNull(streamTrack);
        }

        [UnityTest]
        public IEnumerator When_client_enables_video_during_call_expect_other_client_receiving_video_track()
            => ConnectAndExecute(When_client_enables_video_during_call_expect_other_client_receiving_video_track_Async,
                ignoreFailingMessages: true);

        private async Task When_client_enables_video_during_call_expect_other_client_receiving_video_track_Async(
            ITestClient clientA, ITestClient clientB)
        {
            var streamCall = await clientA.JoinRandomCallAsync();

            var call = await clientB.Client.JoinCallAsync(streamCall.Type, streamCall.Id, create: false, ring: false,
                notify: false);

            var otherParticipant = call.Participants.First(p => !p.IsLocalParticipant);

            await Task.Delay(1000);
            Assert.IsNull(otherParticipant.VideoTrack);

            // Watch other participant video track
            StreamVideoTrack streamTrack = null;
            otherParticipant.TrackAdded += (_, track) => { streamTrack = (StreamVideoTrack)track; };

            // First participant - enable video track
            var cameraDevice = await TestUtils.TryGetFirstWorkingCameraDeviceAsync(clientA.Client);
            clientA.Client.VideoDeviceManager.SelectDevice(cameraDevice, enable: true);

            // Wait for event
            await WaitForConditionAsync(() => streamTrack != null);

            Assert.IsNotNull(streamTrack);
        }

        //StreamTodo: test EndedAt field. (1) is it set when /video/call/{type}/{id}/mark_ended is called, (2) what happens if participants just leave the call
        // (3) if we re-join a previously ended call, is the endedAt null again? 
    }
}
#endif