#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="IosVisionPersonSegmenter"/>. Native Vision create, Metal
    /// mask Y-origin, and lazy request create are iOS-player only.
    /// </summary>
    internal sealed class IosVisionPersonSegmenterTests
    {
        [Test]
        public void When_try_create_in_editor_expect_segmenter_is_not_created()
        {
            var created = IosVisionPersonSegmenter.TryCreate(new UnityLogs(), out var segmenter);

            Assert.That(created, Is.False,
                "Vision segmenter must not initialize in the Editor.");
            Assert.That(segmenter, Is.Null,
                "TryCreate should not return a segmenter when the iOS player path is compiled out.");
        }

        [Test]
        public void When_iphone_x_or_8_expect_fast_quality()
        {
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPhone10,6"), Is.False,
                "iPhone X (A11) must use Fast Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPhone10,1"), Is.False,
                "iPhone 8 (A11) must use Fast Vision quality.");
        }

        [Test]
        public void When_iphone_xs_or_newer_expect_balanced_quality()
        {
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPhone11,2"), Is.True,
                "iPhone XS (A12) must use Balanced Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPhone14,2"), Is.True,
                "iPhone 13 Pro must use Balanced Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPhone17,1"), Is.True,
                "Current-generation iPhones must use Balanced Vision quality.");
        }

        [Test]
        public void When_ipad_generation_expect_quality_matches_ane()
        {
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPad7,5"), Is.False,
                "iPad 6th gen (A10) must use Fast Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPad8,1"), Is.True,
                "iPad Pro 11-inch 1st gen (A12X) must use Balanced Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("iPad13,4"), Is.True,
                "M1 iPad Pro must use Balanced Vision quality.");
        }

        [Test]
        public void When_simulator_or_unknown_identifier_expect_fast_quality()
        {
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("arm64"), Is.False,
                "iOS Simulator utsname must not select Balanced quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality("x86_64"), Is.False,
                "Intel simulator must use Fast Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality(null), Is.False,
                "Missing device model must use Fast Vision quality.");
            Assert.That(IosVisionPersonSegmenter.UsesBalancedQuality(""), Is.False,
                "Empty device model must use Fast Vision quality.");
        }
    }
}
#endif
