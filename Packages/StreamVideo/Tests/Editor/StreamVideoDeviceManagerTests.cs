#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="StreamVideoDeviceManager"/>.
    /// </summary>
    internal sealed class StreamVideoDeviceManagerTests
    {
        [Test]
        public void When_mobile_capture_was_stopped_expect_webcam_is_recreated()
        {
            Assert.That(StreamVideoDeviceManager.ShouldRecreateWebCamTexture(true, false, true), Is.True,
                "Mobile Play() after background does not restore frames; a new WebCamTexture is required.");
        }

        [Test]
        public void When_mobile_first_enable_expect_webcam_is_not_recreated()
        {
            Assert.That(StreamVideoDeviceManager.ShouldRecreateWebCamTexture(true, false, false), Is.False,
                "First Play() must use the existing WebCamTexture; recreating it starts at 16x16.");
        }

        [Test]
        public void When_mobile_camera_playing_expect_webcam_is_not_recreated()
        {
            Assert.That(StreamVideoDeviceManager.ShouldRecreateWebCamTexture(true, true, true), Is.False,
                "A live capture session must not be destroyed every publisher-track change.");
        }

        [Test]
        public void When_editor_camera_stopped_expect_webcam_is_not_recreated()
        {
            Assert.That(StreamVideoDeviceManager.ShouldRecreateWebCamTexture(false, false, true), Is.False,
                "Editor and desktop can Play() the existing WebCamTexture after Stop().");
        }
    }
}
#endif
