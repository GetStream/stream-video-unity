#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PublisherPeerConnection"/>.
    /// </summary>
    internal sealed class PublisherPeerConnectionTests
    {
        [TestCase(1280, 720, 1280, 720)]
        [TestCase(1920, 1080, 1280, 720)]
        [TestCase(1280, 960, 960, 720)]
        [TestCase(1280, 800, 1152, 720)]
        [TestCase(720, 1280, 720, 1280)]
        [TestCase(1080, 1920, 720, 1280)]
        public void When_fitting_camera_resolution_expect_aspect_ratio_kept_within_max(int cameraWidth,
            int cameraHeight, int expectedWidth, int expectedHeight)
        {
            var fitted = PublisherPeerConnection.FitCameraResolution(new VideoResolution(cameraWidth, cameraHeight),
                VideoResolution.Res_720p, PublisherPeerConnection.MinimumSafeTargetResolution);

            AssertResolution(fitted, expectedWidth, expectedHeight,
                $"Camera {cameraWidth}x{cameraHeight} must be scaled down to fit 720p without distortion.");
        }

        [TestCase(640, 480, 768, 576)]
        [TestCase(352, 288, 704, 576)]
        [TestCase(480, 640, 576, 768)]
        public void When_camera_is_below_minimum_expect_scaled_up_with_aspect_ratio_kept(int cameraWidth,
            int cameraHeight, int expectedWidth, int expectedHeight)
        {
            var fitted = PublisherPeerConnection.FitCameraResolution(new VideoResolution(cameraWidth, cameraHeight),
                VideoResolution.Res_720p, PublisherPeerConnection.MinimumSafeTargetResolution);

            AssertResolution(fitted, expectedWidth, expectedHeight,
                $"Camera {cameraWidth}x{cameraHeight} must be scaled up to the encoder-safe minimum without distortion.");
        }

        [Test]
        public void When_aspect_ratio_cannot_fit_between_min_and_max_expect_edges_clamped()
        {
            var fitted = PublisherPeerConnection.FitCameraResolution(new VideoResolution(2560, 720),
                VideoResolution.Res_720p, PublisherPeerConnection.MinimumSafeTargetResolution);

            AssertResolution(fitted, 1280, 576, "Ultra-wide camera must be clamped into the min and max bounds.");
        }

        [Test]
        public void When_max_is_below_min_expect_min()
        {
            var fitted = PublisherPeerConnection.FitCameraResolution(new VideoResolution(1280, 720),
                new VideoResolution(640, 360), PublisherPeerConnection.MinimumSafeTargetResolution);

            AssertResolution(fitted, 704, 576, "The encoder-safe minimum must win over a smaller max.");
        }

        private static void AssertResolution(VideoResolution actual, int expectedWidth, int expectedHeight,
            string message)
        {
            Assert.That(actual.Width, Is.EqualTo((uint)expectedWidth), message);
            Assert.That(actual.Height, Is.EqualTo((uint)expectedHeight), message);
        }
    }
}
#endif
