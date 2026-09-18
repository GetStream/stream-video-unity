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
        public void When_factory_in_editor_expect_unsupported()
        {
            var segmenter = PersonSegmenterFactory.Create(new UnityLogs());

            Assert.That(segmenter.IsSupported, Is.False,
                "Editor factory must not report a person-segmenter backend as supported.");
            Assert.That(segmenter, Is.InstanceOf<NullPersonSegmenter>(),
                "Editor factory must return NullPersonSegmenter, not the ellipse stub.");

            _controller = new BackgroundFilterController(new UnityLogs());
            Assert.That(_controller.IsSupported, Is.False,
                "Default controller in Editor must report unsupported.");

            _controller.SetFilter(BackgroundFilter.Blur(BlurIntensity.Medium));
            Assert.That(_controller.ActiveFilter, Is.Null,
                "SetFilter must no-op when Editor has no person-segmenter backend.");
        }

        [Test]
        public void When_segmenter_create_fails_on_enable_expect_set_filter_is_noop()
        {
            _controller = new BackgroundFilterController(new UnityLogs(), new ResumeFailsPersonSegmenter());

            _controller.SetFilter(BackgroundFilter.Blur(BlurIntensity.Medium));

            Assert.That(_controller.IsSupported, Is.False,
                "Failed ML Kit create must report unsupported.");
            Assert.That(_controller.ActiveFilter, Is.Null,
                "SetFilter must no-op and must not throw when lazy create fails.");
            Assert.That(_controller.IsCompositing, Is.False,
                "Failed create must not composite.");
        }

        [Test]
        public void When_supported_expect_set_and_clear_filter()
        {
            _controller = new BackgroundFilterController(new UnityLogs(), new EditorStubPersonSegmenter());

            var filter = BackgroundFilter.Blur(BlurIntensity.Heavy);
            _controller.SetFilter(filter);

            Assert.That(_controller.IsSupported, Is.True,
                "Injected stub reports supported");
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
        public void When_paused_and_destination_released_expect_composite_passthroughs()
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
                "Precondition: composite should apply with a mask.");

            _controller.Pause();
            _destination.Release();
            _controller.Composite(Texture2D.blackTexture, _destination);

            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Passthrough),
                "Paused Composite must blit when the publisher RT was lost so video can restart.");

            _controller.Resume();
            _controller.Composite(Texture2D.blackTexture, _destination);
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Apply),
                "Resume after a lost publisher RT should composite again.");
        }

        [Test]
        public void When_scheduler_reaches_disable_tier_expect_active_filter_remains_and_composite_does_not_passthrough_live_source()
        {
            var segmenter = new EditorStubPersonSegmenter();
            _controller = new BackgroundFilterController(new UnityLogs(), segmenter);
            var filter = BackgroundFilter.Blur(BlurIntensity.Heavy);
            _controller.SetFilter(filter);

            _destination = CreateDestination();
            segmenter.RequestSegmentation(Texture2D.whiteTexture);
            Assert.That(segmenter.HasMask, Is.True,
                "Stub should produce a mask after RequestSegmentation.");

            _controller.Composite(Texture2D.whiteTexture, _destination);
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Apply),
                "Precondition: composite should apply with a mask.");

            var performanceEvents = 0;
            _controller.PerformanceChanged += _ => performanceEvents++;

            for (var i = 0; i < 3; i++)
            {
                _controller.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);
            }

            Assert.That(_controller.Performance.Degraded, Is.True,
                "Disable tier must still report degraded performance.");
            Assert.That(performanceEvents, Is.GreaterThan(0),
                "BackgroundFilterPerformanceChanged should fire on degrade.");

            _controller.Composite(Texture2D.blackTexture, _destination);

            Assert.That(_controller.ActiveFilter, Is.SameAs(filter),
                "Scheduler disable tier must not clear the requested filter.");
            Assert.That(_controller.LastCompositePath, Is.EqualTo(BackgroundFilterCompositePath.Apply),
                "Disable tier must keep compositing and not blit the live camera.");
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

        private sealed class ResumeFailsPersonSegmenter : IPersonSegmenter
        {
            public bool IsSupported { get; private set; } = true;

            public bool HasMask => false;

            public Texture MaskTexture => null;

            public void RequestSegmentation(Texture source)
            {
            }

            public void PumpPendingMask()
            {
            }

            public void Pause()
            {
            }

            public void Resume()
            {
                IsSupported = false;
            }

            public void Dispose()
            {
            }
        }

        private BackgroundFilterController _controller;
        private RenderTexture _destination;
    }
}
#endif
