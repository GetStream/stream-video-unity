#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.BackgroundFilters;
using StreamVideo.Libs.Logs;
using UnityEngine;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="BackgroundFilterController"/>.
    /// </summary>
    internal sealed class BackgroundFilterControllerTests
    {
        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _controller = null;
        }

        [Test]
        public void When_unsupported_platform_expect_set_filter_is_noop()
        {
            _controller = new BackgroundFilterController(new UnityLogs(), new NullPersonSegmenter());

            _controller.SetFilter(BackgroundFilter.Blur(BlurIntensity.Medium));

            Assert.That(_controller.IsSupported, Is.False,
                "Null segmenter should report unsupported.");
            Assert.That(_controller.ActiveFilter, Is.Null,
                "SetBackgroundFilter should no-op when the platform is unsupported.");
            Assert.That(_controller.IsCompositing, Is.False,
                "Unsupported controller must not composite.");
        }

        [Test]
        public void When_supported_expect_set_and_clear_filter()
        {
            _controller = new BackgroundFilterController(new UnityLogs(), new EditorStubPersonSegmenter());

            var filter = BackgroundFilter.Blur(BlurIntensity.Heavy);
            _controller.SetFilter(filter);

            Assert.That(_controller.IsSupported, Is.True,
                "Editor stub segmenter should be supported.");
            Assert.That(_controller.ActiveFilter, Is.SameAs(filter),
                "Supported controller should keep the requested filter.");
            Assert.That(_controller.ActiveFilter.Intensity, Is.EqualTo(BlurIntensity.Heavy),
                "Blur intensity should match the request.");

            _controller.SetFilter(null);

            Assert.That(_controller.ActiveFilter, Is.Null,
                "Passing null should disable the filter.");
            Assert.That(_controller.IsCompositing, Is.False,
                "Disabled filter must not composite.");
        }

        [Test]
        public void When_pause_then_resume_expect_compositing_requires_mask()
        {
            var segmenter = new EditorStubPersonSegmenter();
            _controller = new BackgroundFilterController(new UnityLogs(), segmenter);
            _controller.SetFilter(BackgroundFilter.Blur());

            _controller.Pause();
            Assert.That(_controller.IsCompositing, Is.False,
                "Paused controller should not composite.");

            _controller.Resume();
            Assert.That(_controller.IsCompositing, Is.False,
                "Resume without a generated mask should still wait for the first mask.");

            segmenter.RequestSegmentation(Texture2D.whiteTexture);
            Assert.That(segmenter.HasMask, Is.True,
                "Stub should produce a mask after RequestSegmentation.");
            Assert.That(_controller.IsCompositing, Is.True,
                "Once a mask exists, a resumed active filter should composite.");
        }

        private BackgroundFilterController _controller;
    }
}
#endif
