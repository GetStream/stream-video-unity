#if STREAM_TESTS_ENABLED
using System.IO;
using NUnit.Framework;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for WebGL platform enablement in the vendored WebRTC package.
    /// </summary>
    internal sealed class WebGLSupportTests
    {
        [Test]
        public void When_webrtc_runtime_asmdef_expect_webgl_included()
        {
            var path = ResolveWebRtcAsmdefPath();
            Assert.That(File.Exists(path), Is.True,
                $"Expected WebRTC runtime asmdef at {path}.");

            var json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("\"WebGL\""),
                "Stream.Unity.WebRTC.Runtime.asmdef must include the WebGL platform so browser player builds compile the package.");
        }

        [Test]
        public void When_webrtc_webgl_plugins_exist_expect_jslib_present()
        {
            var pluginsDir = Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL");
            Assert.That(Directory.Exists(pluginsDir), Is.True,
                $"Expected WebGL plugin directory at {pluginsDir}.");
            Assert.That(File.Exists(Path.Combine(pluginsDir, "RTCPeerConnection.jslib")), Is.True,
                "RTCPeerConnection.jslib is required for browser WebRTC.");
            Assert.That(File.Exists(Path.Combine(pluginsDir, "VideoStreamTrack.jslib")), Is.True,
                "VideoStreamTrack.jslib is required for canvas capture and remote video.");
            Assert.That(File.Exists(Path.Combine(pluginsDir, "AudioStreamTrack.jslib")), Is.True,
                "AudioStreamTrack.jslib is required for local audio capture.");
        }

        static string ResolveWebRtcAsmdefPath()
            => Path.Combine(ResolveWebRtcRuntimeDir(), "Stream.Unity.WebRTC.Runtime.asmdef");

        static string ResolveWebRtcRuntimeDir()
        {
            var candidates = new[]
            {
                Path.GetFullPath("Packages/io.getstream.video/Runtime/Libs/io.stream.unity.webrtc/Runtime"),
                Path.GetFullPath("Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/Runtime")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }
    }
}
#endif
