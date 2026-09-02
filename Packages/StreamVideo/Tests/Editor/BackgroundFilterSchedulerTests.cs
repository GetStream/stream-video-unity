#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core;
using StreamVideo.Core.BackgroundFilters;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="FilterFrameScheduler"/>.
    /// </summary>
    internal sealed class BackgroundFilterSchedulerTests
    {
        [SetUp]
        public void SetUp()
        {
            _scheduler = new FilterFrameScheduler();
            _scheduler.Reset(BlurIntensity.Medium);
        }

        [Test]
        public void When_default_state_expect_segment_every_second_frame()
        {
            Assert.That(_scheduler.ShouldSegment(0), Is.True,
                "Frame 0 should request segmentation.");
            Assert.That(_scheduler.ShouldSegment(1), Is.False,
                "Frame 1 should reuse the last mask.");
            Assert.That(_scheduler.ShouldSegment(2), Is.True,
                "Frame 2 should request segmentation.");
        }

        [Test]
        public void When_low_fps_ratio_for_hold_period_expect_interval_increases()
        {
            _scheduler.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);

            Assert.That(_scheduler.SegmentIntervalFrames, Is.EqualTo(3),
                "Sustained low FPS should drop segmentation to every 3rd frame.");
            Assert.That(_scheduler.Performance.Degraded, Is.True,
                "Sustained low FPS should mark the filter as degraded.");
            Assert.That(_scheduler.ShouldDisable, Is.False,
                "First degrade step should not disable the filter.");
        }

        [Test]
        public void When_low_fps_ratio_continues_expect_blur_becomes_light_then_disables()
        {
            _scheduler.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);
            _scheduler.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);

            Assert.That(_scheduler.EffectiveIntensity, Is.EqualTo(BlurIntensity.Light),
                "Second degrade step should force Light blur.");

            _scheduler.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);

            Assert.That(_scheduler.ShouldDisable, Is.True,
                "Third degrade step should disable the filter.");
            Assert.That(_scheduler.Performance.Reason, Is.EqualTo(BackgroundFilterDegradeReason.FrameDrop),
                "Disable should report a frame-drop reason.");
        }

        [Test]
        public void When_fps_ratio_recovers_expect_quality_restored()
        {
            _scheduler.RecordFpsRatio(0.5f, FilterFrameScheduler.DegradeHoldSeconds);
            Assert.That(_scheduler.SegmentIntervalFrames, Is.EqualTo(3),
                "Precondition: scheduler should already be degraded.");

            _scheduler.RecordFpsRatio(0.95f, FilterFrameScheduler.DegradeHoldSeconds);

            Assert.That(_scheduler.SegmentIntervalFrames, Is.EqualTo(FilterFrameScheduler.DefaultSegmentIntervalFrames),
                "Sustained high FPS should restore the default segment interval.");
            Assert.That(_scheduler.Performance.Degraded, Is.False,
                "Recovered scheduler should not stay marked degraded.");
        }

        [Test]
        public void When_fps_ratio_is_in_hysteresis_band_expect_tier_unchanged()
        {
            _scheduler.RecordFpsRatio(0.8f, FilterFrameScheduler.DegradeHoldSeconds);

            Assert.That(_scheduler.QualityTier, Is.EqualTo(0),
                "Ratios between 0.75 and 0.85 should not change quality.");
        }

        private FilterFrameScheduler _scheduler;
    }
}
#endif
