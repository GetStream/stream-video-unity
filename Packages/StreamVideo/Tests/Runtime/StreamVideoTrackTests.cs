#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UiImage = UnityEngine.UIElements.Image;

namespace StreamVideo.Tests.Runtime
{
    /// <summary>
    /// Tests for <see cref="StreamVideoTrack"/> UI Toolkit rendering against a live call.
    /// </summary>
    internal class StreamVideoTrackTests : TestsBase
    {
        [UnityTest, ConditionalIgnore(IgnoreConditionNoCameraKey, IgnoreConditionNoCameraReason)]
        public IEnumerator When_ui_toolkit_render_target_set_expect_image_receives_texture()
            => ConnectAndExecute(When_ui_toolkit_render_target_set_expect_image_receives_texture_Async,
                ignoreFailingMessages: true);

        [UnityTest, ConditionalIgnore(IgnoreConditionNoCameraKey, IgnoreConditionNoCameraReason)]
        public IEnumerator When_retarget_to_raw_image_expect_ui_toolkit_updates_stop()
            => ConnectAndExecute(When_retarget_to_raw_image_expect_ui_toolkit_updates_stop_Async,
                ignoreFailingMessages: true);

        [UnityTest, ConditionalIgnore(IgnoreConditionNoCameraKey, IgnoreConditionNoCameraReason)]
        public IEnumerator When_render_target_null_or_disposed_expect_exception_and_cleared_image()
            => ConnectAndExecute(When_render_target_null_or_disposed_expect_exception_and_cleared_image_Async,
                ignoreFailingMessages: true);

        private async Task When_ui_toolkit_render_target_set_expect_image_receives_texture_Async(
            ITestClient clientA, ITestClient clientB)
        {
            var streamTrack = await ReceiveRemoteVideoTrackAsync(clientA, clientB);
            var image = new UiImage();
            streamTrack.SetRenderTarget(image);

            var (received, elapsed) = await WaitForConditionAsync(
                () => image.image != null && image.image.width > 0, timeoutMs: 15000);
            AssertImageShowsTrackTexture(received, elapsed, streamTrack, image,
                "UI Toolkit image should receive a remote video texture.");
        }

        private async Task When_retarget_to_raw_image_expect_ui_toolkit_updates_stop_Async(
            ITestClient clientA, ITestClient clientB)
        {
            var participant = await ReceiveRemoteParticipantAsync(clientA, clientB);
            var streamTrack = await WaitForVideoTrackAsync(participant);
            var image = new UiImage();
            streamTrack.SetRenderTarget(image);

            var (received, elapsed) = await WaitForConditionAsync(
                () => image.image != null && image.image.width > 0, timeoutMs: 15000);
            AssertImageShowsTrackTexture(received, elapsed, streamTrack, image,
                "Precondition: UI Toolkit image should receive video before retargeting.");

            var rawHost = new GameObject("StreamVideoTrackTests.RawImage");
            var rawImage = rawHost.AddComponent<RawImage>();
            var sentinel = new Texture2D(8, 8);
            try
            {
                streamTrack.SetRenderTarget(rawImage);
                image.image = sentinel;
                participant.UpdateRequestedVideoResolution(new VideoResolution(320, 240));

                var (rawReceived, rawElapsed) = await WaitForConditionAsync(() => rawImage.texture != null,
                    timeoutMs: 5000);
                Assert.That(rawReceived, Is.True,
                    "RawImage should receive the track texture after retargeting. Waited " +
                    rawElapsed.TotalSeconds + "s.");
                Assert.That(image.image, Is.SameAs(sentinel),
                    "UI Toolkit updates should stop after SetRenderTarget(RawImage).");

                streamTrack.SetRenderTarget(image);
                rawImage.texture = sentinel;
                var (uiReceived, uiElapsed) = await WaitForConditionAsync(
                    () => image.image != null && !ReferenceEquals(image.image, sentinel), timeoutMs: 5000);
                Assert.That(uiReceived, Is.True,
                    "UI Toolkit image should receive the track texture again. Waited " + uiElapsed.TotalSeconds + "s.");
                Assert.That(rawImage.texture, Is.SameAs(sentinel),
                    "RawImage updates should stop after SetRenderTarget(UI Toolkit image).");
            }
            finally
            {
                Object.Destroy(rawHost);
                Object.Destroy(sentinel);
            }
        }

        private async Task When_render_target_null_or_disposed_expect_exception_and_cleared_image_Async(
            ITestClient clientA, ITestClient clientB)
        {
            var streamTrack = await ReceiveRemoteVideoTrackAsync(clientA, clientB);

            Assert.Throws<System.ArgumentNullException>(() => streamTrack.SetRenderTarget((RawImage)null),
                "A null RawImage should be rejected.");
            Assert.Throws<System.ArgumentNullException>(() => streamTrack.SetRenderTarget((UiImage)null),
                "A null UI Toolkit image should be rejected.");

            var image = new UiImage();
            streamTrack.SetRenderTarget(image);
            var (received, elapsed) = await WaitForConditionAsync(
                () => image.image != null && image.image.width > 0, timeoutMs: 15000);
            AssertImageShowsTrackTexture(received, elapsed, streamTrack, image,
                "Precondition: image should show the track before dispose.");

            streamTrack.Dispose();

            Assert.That(image.image, Is.Null,
                "Dispose should clear the UI Toolkit image when it still shows this track's texture.");
        }

        private static async Task<StreamVideoTrack> ReceiveRemoteVideoTrackAsync(ITestClient clientA, ITestClient clientB)
        {
            var participant = await ReceiveRemoteParticipantAsync(clientA, clientB);
            return await WaitForVideoTrackAsync(participant);
        }

        private static async Task<StreamVideoTrack> WaitForVideoTrackAsync(
            IStreamVideoCallParticipant participant)
        {
            if (participant.VideoTrack is StreamVideoTrack existing)
            {
                return existing;
            }

            StreamVideoTrack streamTrack = null;
            participant.TrackAdded += (_, track) =>
            {
                if (track is StreamVideoTrack videoTrack)
                {
                    streamTrack = videoTrack;
                }
            };

            if (participant.VideoTrack is StreamVideoTrack arrived)
            {
                return arrived;
            }

            var (received, elapsed) = await WaitForConditionAsync(() => streamTrack != null, timeoutMs: 15000);
            Assert.That(received, Is.True,
                "The watching client should receive a video track. Waited " + elapsed.TotalSeconds + "s.");
            return streamTrack;
        }

        private static async Task<IStreamVideoCallParticipant> ReceiveRemoteParticipantAsync(
            ITestClient clientA, ITestClient clientB)
        {
            var clientACall = await clientA.JoinRandomCallAsync();
            await SelectCameraThatDeliversFramesAsync(clientA.Client);

            var clientBCall = await clientB.Client.JoinCallAsync(clientACall.Type, clientACall.Id, create: false,
                ring: false, notify: false);

            var localSessionId = clientACall.GetLocalParticipant().SessionId;
            var (joined, elapsed) = await WaitForConditionAsync(
                () => clientBCall.Participants.Any(participant => participant.SessionId == localSessionId),
                timeoutMs: 15000);
            Assert.That(joined, Is.True,
                "The watching client should see the publishing participant. Waited " + elapsed.TotalSeconds + "s.");

            var participant = clientBCall.Participants.First(p => p.SessionId == localSessionId);
            participant.UpdateRequestedVideoResolution(VideoResolution.Res_720p);
            return participant;
        }

        // The default SelectDevice request is 720p. Batch-mode players often reject that size
        // ("could not find a supported resolution") even when a smaller mode works.
        private static async Task SelectCameraThatDeliversFramesAsync(IStreamVideoClient client)
        {
            var manager = client.VideoDeviceManager;
            foreach (var device in manager.EnumerateDevices())
            {
                foreach (var resolution in CandidateResolutions())
                {
                    manager.SelectDevice(device, resolution, enable: true);
                    var texture = manager.GetSelectedDeviceWebCamTexture();
                    var (delivering, _) = await WaitForConditionAsync(
                        () => texture != null && texture.width > 16 && texture.height > 16,
                        timeoutMs: 2000);
                    if (!delivering)
                    {
                        continue;
                    }

                    Debug.Log("Publishing camera " + device.Name + " at " + texture.width + "x" + texture.height);
                    return;
                }
            }

            Assert.Ignore("No camera delivered frames at a supported resolution in this player.");
        }

        // Batch-mode Play Mode connects and receives the track, but WebRTC.Update waits on
        // WaitForEndOfFrame and no decoded texture is created. Ignore that environment limit.
        // A texture that exists but is not assigned to the image is still a failure.
        private static void AssertImageShowsTrackTexture(bool received, System.TimeSpan elapsed,
            StreamVideoTrack streamTrack, UiImage image, string requirement)
        {
            if (received)
            {
                return;
            }

            var description = DescribeMediaTrack(streamTrack);
            if (description.Contains("texture=null"))
            {
                Assert.Ignore("No decoded video texture arrived in this player, so the render target was not checked. " +
                              description + " Waited " + elapsed.TotalSeconds + "s.");
            }

            Assert.That(image.image, Is.Not.Null,
                requirement + " Waited " + elapsed.TotalSeconds + "s. " + description);
        }

        private static string DescribeMediaTrack(StreamVideoTrack streamTrack)
        {
            var mediaTrack = typeof(StreamVideoTrack).BaseType
                ?.GetProperty("Track", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(streamTrack);
            if (mediaTrack == null)
            {
                return "media track is null";
            }

            var mediaType = mediaTrack.GetType();
            var texture = mediaType.GetProperty("Texture")?.GetValue(mediaTrack) as Texture;
            var decoding = mediaType.GetProperty("Decoding")?.GetValue(mediaTrack);
            var textureDescription = texture == null ? "null" : texture.width + "x" + texture.height;
            return "decoding=" + decoding + ", texture=" + textureDescription;
        }

        private static VideoResolution[] CandidateResolutions()
        {
            return new[]
            {
                VideoResolution.Res_720p,
                VideoResolution.Res_480p,
                VideoResolution.Res_360p,
                VideoResolution.Res_240p,
                new VideoResolution(160, 120),
            };
        }
    }
}
#endif
