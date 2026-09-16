#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.BackgroundFilters;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="AndroidMlKitPersonSegmenter"/>.
    /// Android teardown cannot run here: Dispose must not block the game thread, must not destroy
    /// a RenderTexture with a pending <c>AsyncGPUReadback</c>, and must not recycle the ML Kit
    /// bitmap until <c>process</c> finishes. In-flight GPU/Java work releases its own resources.
    /// Manual: enable blur, go to background / battery saver, dispose or quit while moving,
    /// confirm no ANR and no crash in logcat.
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
