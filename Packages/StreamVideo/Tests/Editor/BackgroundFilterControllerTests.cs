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
    /// Android ML Kit teardown is device-only and is not covered here. Dispose must not block
    /// or hang if GPU/ML Kit are delayed (background, battery saver). Manual: enable blur,
    /// background or dispose while moving, confirm no ANR and no crash in logcat.
    /// </summary>
    internal sealed class BackgroundFilterControllerTests
    {
        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _controller = null;

            if (_destination != null)
            {
                _destination.Release();
                UnityEngine.Object.DestroyImmediate(_destination);
                _destination = null;
            }
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

        [Test]
        public void When_paused_with_active_filter_and_mask_expect_composite_does_not_overwrite_destination_with_source()
        {
            var segmenter = new EditorStubPersonSegmenter();
            _controller = new BackgroundFilterController(new UnityLogs(), segmenter);
            _controller.SetFilter(BackgroundFilter.Blur());

            _destination = CreateDestination();
            segmenter.RequestSegmentation(Texture2D.whiteTexture);
            Assert.That(segmenter.HasMask, Is.True,
                "Stub should produce a mask after RequestSegmentation.");

            _controller.Composite(Texture2D.whiteTexture, _destination);
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Apply),
                "Composite with an active filter and mask should apply before pause.");

            _controller.Pause();
            _controller.Composite(Texture2D.blackTexture, _destination);

            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Frozen),
                "Paused Composite must freeze the last composited frame and not blit the live camera.");

            _controller.Resume();
            _controller.Composite(Texture2D.blackTexture, _destination);
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Apply),
                "Resume should continue compositing without a new SetFilter.");
        }

        [Test]
        public void When_filter_disabled_expect_composite_passthrough_still_blits_source()
        {
            _controller = new BackgroundFilterController(new UnityLogs(), new EditorStubPersonSegmenter());
            _destination = CreateDestination();

            _controller.Composite(Texture2D.whiteTexture, _destination);

            Assert.That(_controller.ActiveFilter, Is.Null,
                "Filter should remain off until SetFilter is called.");
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Passthrough),
                "Filter-off Composite must still blit source to destination.");
        }

        private static RenderTexture CreateDestination()
        {
            var destination = new RenderTexture(16, 16, 0, RenderTextureFormat.ARGB32);
            destination.Create();
            return destination;
        }

        private BackgroundFilterController _controller;
        private RenderTexture _destination;
    }
}
#endif
