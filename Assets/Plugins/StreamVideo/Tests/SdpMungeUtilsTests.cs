using System.Linq;
using NUnit.Framework;
using StreamVideo.Core.LowLevelClient;

//StreamTodo: wrap in compiler flag
namespace StreamVideo.Tests
{
    internal sealed class SdpMungeUtilsTests
    {
        [Test]
        public void When_enabled_red_expect_red_codec_in_front_of_the_list()
        {
            var utils = new SdpMungeUtils();
            var modifiedSdp = utils.ModifySdp(_sampleSdp, enableRed: true, enableDtx: false);
            
            var lines = modifiedSdp.Split("\n");
            var mRecord = lines.Single(l => l.StartsWith("m=audio"));
            var mRecordParts = mRecord.Remove(0, "m=audio 9 UDP/TLS/RTP/SAVPF".Length).Trim();
            var codecs = mRecordParts.Split(" ").Select(int.Parse);
            
            Assert.That(codecs.First() == 97);

        }

        private const string _sampleSdp = @"v=0
o=- 5881996535939993027 2 IN IP4 127.0.0.1
s=-
t=0 0
a=group:BUNDLE 0 1
a=extmap-allow-mixed
a=msid-semantic: WMS 840f84e3-b268-4557-a506-1963254efcee:1:7 840f84e3-b268-4557-a506-1963254efcee:2:4
m=audio 9 UDP/TLS/RTP/SAVPF 96 97 98 99 102 9 0 8 100 101 107 108 109 114 106 105 13 110 112 113 126
c=IN IP4 0.0.0.0
a=rtcp:9 IN IP4 0.0.0.0
a=ice-ufrag:Uumv
a=ice-pwd:P37BykcNMzZb0JVKKPVjxwy7
a=ice-options:trickle
a=fingerprint:sha-256 99:7D:98:CD:AE:19:33:36:7D:1C:0A:9E:EB:E9:F1:AA:FA:2F:B5:7A:54:37:D0:E3:42:A4:6D:AD:53:99:85:D6
a=setup:actpass
a=mid:0
a=extmap:1 urn:ietf:params:rtp-hdrext:ssrc-audio-level
a=extmap:2 http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time
a=extmap:3 http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01
a=extmap:4 urn:ietf:params:rtp-hdrext:sdes:mid
a=sendonly
a=msid:840f84e3-b268-4557-a506-1963254efcee:1:7 3c4431fe-b66a-48d4-8d03-afb0a0514a38
a=rtcp-mux
a=rtpmap:96 opus/48000/2
a=rtcp-fb:96 transport-cc
a=fmtp:96 minptime=10;sprop-stereo=1;stereo=1;useinbandfec=1
a=rtpmap:97 red/48000/2
a=fmtp:97 96/96
a=rtpmap:98 multiopus/48000/6
a=fmtp:98 channel_mapping=0,4,1,2,3,5;coupled_streams=2;minpuseinbandfec
a=fmtp:99 channel_mapping=0,6,1,2,3,4,5,7;coupled_streams=3;minptime=10;num_streams=5;useinbandfec=1
a=rtpmap:102 ILBC/8000
a=rtpmap:9 G722/8000
a=rtpmap:0 PCMU/8000
a=rtpmap:8 PCMA/8000
a=rtpmap:100 L16/8000
a=rtpmap:101 L16/16000
a=rtpmap:107 L16/32000
a=rtpmap:108 L16/8000/2
a=rtpmap:109 L16/16000/2
a=rtpmap:114 L16/32000/2
a=rtpmap:106 CN/32000
a=rtpmap:105 CN/16000
a=rtpmap:13 CN/8000
a=rtpmap:110 telephone-event/48000
a=rtpmap:112 telephone-event/32000
a=rtpmap:113 telephone-event/16000
a=rtpmap:126 telephone-event/8000
a=ssrc:3807602524 cname:1t2nQ1AyGFySkk8k
a=ssrc:3807602524 msid:840f84e3-b268-4557-a506-1963254efcee:1:7 3c4431fe-b66a-48d4-8d03-afb0a0514a38
m=video 9 UDP/TLS/RTP/SAVPF 123 104 122 121
c=IN IP4 0.0.0.0
a=rtcp:9 IN IP4 0.0.0.0
a=ice-ufrag:Uumv
a=ice-pwd:P37BykcNMzZb0JVKKPVjxwy7
a=ice-options:trickle
a=fingerprint:sha-256 99:7D:98:CD:AE:19:33:36:7D:1C:0A:9E:EB:E9:F1:AA:FA:2F:B5:7A:54:37:D0:E3:42:A4:6D:AD:53:99:85:D6
a=setup:actpass
a=mid:1
a=extmap:14 urn:ietf:params:rtp-hdrext:toffset
a=extmap:2 http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time
a=extmap:13 urn:3gpp:video-orientation
a=extmap:3 http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01
a=extmap:5 http://www.webrtc.org/experiments/rtp-hdrext/playout-delay
a=extmap:6 http://www.webrtc.org/experiments/rtp-hdrext/video-content-type
a=extmap:7 http://www.webrtc.org/experiments/rtp-hdrext/video-timing
a=extmap:8 http://www.webrtc.org/experiments/rtp-hdrext/color-space
a=extmap:4 urn:ietf:params:rtp-hdrext:sdes:mid
a=extmap:10 urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id
a=extmap:11 urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id
a=sendonly
a=msid:840f84e3-b268-4557-a506-1963254efcee:2:4 6e199c0e-70f6-4602-88e9-8f222f0961ac
a=rtcp-mux
a=rtcp-rsize
a=rtpmap:123 H264/90000
a=rtcp-fb:123 goog-remb
a=rtcp-fb:123 transport-cc
a=rtcp-fb:123 ccm fir
a=rtcp-fb:123 nack
a=rtcp-fb:123 nack pli
a=fmtp:123 implementation_name=NvCodec;level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=42e033
a=rtpmap:104 H264/90000
a=rtcp-fb:104 goog-remb
a=rtcp-fb:104 transport-cc
a=rtcp-fb:104 ccm fir
a=rtcp-fb:104 nack
a=rtcp-fb:104 nack pli
a=fmtp:104 implementation_name=NvCodec;level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=420033
a=rtpmap:122 H264/90000
a=rtcp-fb:122 goog-remb
a=rtcp-fb:122 transport-cc
a=rtcp-fb:122 ccm fir
a=rtcp-fb:122 nack
a=rtcp-fb:122 nack pli
a=fmtp:122 implementation_name=NvCodec;level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=640033
a=rtpmap:121 H264/90000
a=rtcp-fb:121 goog-remb
a=rtcp-fb:121 transport-cc
a=rtcp-fb:121 ccm fir
a=rtcp-fb:121 nack
a=rtcp-fb:121 nack pli
a=fmtp:121 implementation_name=NvCodec;level-asymmetry-allowed=1;packetization-mode=1;profile-level-id=4d0033
a=rid:q send
a=rid:h send
a=rid:f send
a=simulcast:send q;h;f";
    }
}