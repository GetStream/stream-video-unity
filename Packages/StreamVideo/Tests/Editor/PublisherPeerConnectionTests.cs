#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="PublisherPeerConnection"/> SDP track-id parsing.
    /// </summary>
    internal sealed class PublisherPeerConnectionTests
    {
        [Test]
        public void When_sdp_msid_matches_stream_id_expect_track_id()
        {
            var found = PublisherPeerConnection.TryExtractMsidTrackId(
                NativeStyleSdp, "840f84e3-b268-4557-a506-1963254efcee:1:7", "video", out var trackId);

            Assert.That(found, Is.True);
            Assert.That(trackId, Is.EqualTo("6e199c0e-70f6-4602-88e9-8f222f0961ac"));
        }

        [Test]
        public void When_csharp_guid_does_not_match_browser_stream_id_expect_video_section_track_id()
        {
            var foundByGuid = PublisherPeerConnection.TryExtractMsidTrackId(
                WebGlSdp, "fa58172b-590a-4ff7-8a01-d44c06aaf460", "video", out _);
            Assert.That(foundByGuid, Is.False,
                "WebGL cannot write MediaStream.id, so the C# guid is not in SDP a=msid.");

            var found = PublisherPeerConnection.TryExtractMsidTrackId(
                WebGlSdp, mediaStreamId: null, "video", out var trackId);

            Assert.That(found, Is.True);
            Assert.That(trackId, Is.EqualTo("2cfc744a-1cb4-4e61-9c30-c9987d878c24"));
        }

        [Test]
        public void When_extracting_video_msid_expect_audio_section_ignored()
        {
            var found = PublisherPeerConnection.TryExtractMsidTrackId(
                NativeStyleSdp, mediaStreamId: null, "video", out var trackId);

            Assert.That(found, Is.True);
            Assert.That(trackId, Is.EqualTo("6e199c0e-70f6-4602-88e9-8f222f0961ac"));
            Assert.That(trackId, Is.Not.EqualTo("3c4431fe-b66a-48d4-8d03-afb0a0514a38"),
                "Must not pick the audio a=msid track id.");
        }

        private const string NativeStyleSdp = @"v=0
o=- 5881996535939993027 2 IN IP4 127.0.0.1
s=-
t=0 0
a=msid-semantic: WMS 840f84e3-b268-4557-a506-1963254efcee:1:7
m=audio 9 UDP/TLS/RTP/SAVPF 96
a=mid:0
a=msid:840f84e3-b268-4557-a506-1963254efcee:1:7 3c4431fe-b66a-48d4-8d03-afb0a0514a38
m=video 9 UDP/TLS/RTP/SAVPF 96
a=mid:1
a=msid:840f84e3-b268-4557-a506-1963254efcee:1:7 6e199c0e-70f6-4602-88e9-8f222f0961ac
";

        private const string WebGlSdp = @"v=0
o=- 8851030162515913690 3 IN IP4 127.0.0.1
s=-
t=0 0
a=msid-semantic: WMS fa2c081e-1ca4-4b48-85cc-7ca5f7568ee4
m=video 9 UDP/TLS/RTP/SAVPF 96
a=mid:0
a=msid:fa2c081e-1ca4-4b48-85cc-7ca5f7568ee4 2cfc744a-1cb4-4e61-9c30-c9987d878c24
a=ssrc:2376266259 msid:fa2c081e-1ca4-4b48-85cc-7ca5f7568ee4 2cfc744a-1cb4-4e61-9c30-c9987d878c24
";
    }
}
#endif
