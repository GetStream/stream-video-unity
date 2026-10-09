#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="RtcSession"/> mute-state generation.
    /// </summary>
    internal sealed class RtcSessionMuteGenerationTests
    {
        [Test]
        public void When_generation_matches_expect_mute_state_is_sent()
        {
            Assert.That(RtcSession.IsLatestMuteGeneration(2, 2), Is.True,
                "The newest enable or disable must be sent to the SFU.");
        }

        [Test]
        public void When_generation_is_stale_expect_mute_state_is_dropped()
        {
            Assert.That(RtcSession.IsLatestMuteGeneration(1, 2), Is.False,
                "A background mute that finishes after resume must not overwrite the unmute.");
        }
    }
}
#endif
