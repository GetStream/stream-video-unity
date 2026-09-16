#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="AndroidMlKitPersonSegmenter"/>. Native teardown, upright ML Kit
    /// inference (portrait/landscape, front/back), and GLES vs Vulkan mask Y-origin are
    /// Android-player only.
    /// </summary>
    internal sealed class AndroidMlKitPersonSegmenterTests
    {
        [Test]
        public void When_try_create_in_editor_expect_segmenter_is_not_created()
        {
            var created = AndroidMlKitPersonSegmenter.TryCreate(new UnityLogs(), out var segmenter);

            Assert.That(created, Is.False,
                "ML Kit segmenter must not initialize in the Editor.");
            Assert.That(segmenter, Is.Null,
                "TryCreate should not return a segmenter when the Android player path is compiled out.");
        }
    }
}
#endif
