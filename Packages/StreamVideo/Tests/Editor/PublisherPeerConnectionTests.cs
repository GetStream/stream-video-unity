#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PublisherPeerConnection"/>.
    /// </summary>
    internal sealed class PublisherPeerConnectionTests
    {
        [Test]
        public void When_webcam_is_placeholder_size_expect_capture_is_not_usable()
        {
            Assert.That(PublisherPeerConnection.HasUsableCaptureSize(16, 16), Is.False,
                "Unity WebCamTexture starts at 16x16 before the capture session is ready.");
        }

        [Test]
        public void When_existing_track_and_placeholder_size_expect_publisher_track_is_replaced()
        {
            Assert.That(
                PublisherPeerConnection.ShouldReplacePublisherVideoTrackOnInputChanged(true, 16, 16),
                Is.True,
                "iOS camera restart after background is still 16x16; the encode track must be replaced to rebind GPU textures.");
        }

        [Test]
        public void When_no_track_and_placeholder_size_expect_publisher_track_is_not_replaced()
        {
            Assert.That(
                PublisherPeerConnection.ShouldReplacePublisherVideoTrackOnInputChanged(false, 16, 16),
                Is.False,
                "First camera bind before capture starts must not create a publisher track at 16x16.");
        }
    }
}
#endif
