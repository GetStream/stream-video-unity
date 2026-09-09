#if STREAM_TESTS_ENABLED
using System;
using System.IO;
using NUnit.Framework;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for WebGL platform enablement.
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

        [Test]
        public void When_unity_web_request_http_client_source_expect_json_content_type()
        {
            var path = ResolveUnityWebRequestHttpClientPath();
            Assert.That(File.Exists(path), Is.True,
                $"Expected UnityWebRequestHttpClient at {path}.");

            var source = File.ReadAllText(path);
            Assert.That(source, Does.Contain("application/json"),
                "WebGL UnityWebRequest must set JSON Content-Type. The default application/octet-stream is rejected or hidden by CORS in the browser as Unknown Error.");
            Assert.That(source, Does.Contain("RequestTimeoutSeconds"),
                "WebGL UnityWebRequest must use a timeout long enough for CORS preflight + POST. A 5s timeout surfaces as Unknown Error.");
            Assert.That(source, Does.Contain("MethodAllowsRequestBody"),
                "WebGL UnityWebRequest must not attach a JSON body to GET. Browsers reject GET bodies and Unity reports ConnectionError / Unknown Error.");
        }

        [Test]
        public void When_audio_device_manager_source_expect_unity_microphone_guarded_from_webgl_player()
        {
            var path = ResolveAudioDeviceManagerPath();
            Assert.That(File.Exists(path), Is.True,
                $"Expected StreamAudioDeviceManager at {path}.");

            var source = File.ReadAllText(path);
            Assert.That(source, Does.Contain("#if UNITY_WEBGL && !UNITY_EDITOR"),
                "StreamAudioDeviceManager must exclude UnityEngine.Microphone from WebGL player compiles. That type is not in the WebGL player API.");
            Assert.That(source, Does.Contain("Microphone.devices"),
                "Non-WebGL path should still enumerate Unity microphone devices.");
        }

        [Test]
        public void When_webgl_create_offer_source_expect_pending_sdp_pump()
        {
            var sessionOps = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Scripts", "WebGLSessionOps.cs"));
            Assert.That(sessionOps, Does.Contain("PumpPendingSessionOps"),
                "WebGL CreateOffer must complete observers from Unity's update loop. Promise dynCall into IL2CPP with marshaled strings does not reliably finish the C# await.");
            Assert.That(sessionOps, Does.Contain("PeerConnectionTakePendingCreateSd"),
                "Pump must read the SDP stored on the JS peer after createOffer resolves.");
            Assert.That(sessionOps, Does.Contain("OnCreateSuccess(IntPtr peerPtr, int sdpType, IntPtr sdpPtr)"),
                "WebGL CreateOffer callbacks must take IntPtr, not string. IL2CPP reverse P/Invoke string marshalling from a Promise hangs the join.");

            var jslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "RTCPeerConnection.jslib"));
            Assert.That(jslib, Does.Contain("_pendingCreateSd"),
                "createOffer must store SDP on the peer so C# can pump it from Unity's update loop.");
            Assert.That(jslib, Does.Contain("PeerConnectionTakePendingCreateSd"),
                "jslib must export TakePendingCreateSd for the WebGL pump.");
            Assert.That(jslib, Does.Not.Contain("uwcom_dynCall('viii', uwevt_OnSuccessCreateSessionDesc"),
                "createOffer/createAnswer must not dynCall into IL2CPP from a Promise. That hangs the WebGL player after the offer is created.");
            Assert.That(jslib, Does.Not.Contain("uwcom_dynCall('vi', uwevt_OnSetSessionDescSuccess"),
                "setLocalDescription/setRemoteDescription must not dynCall into IL2CPP from a Promise.");

            var webrtc = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Scripts", "WebRTC.cs"));
            Assert.That(webrtc, Does.Contain("PumpPendingSessionOps"),
                "WebRTC.Update must pump pending WebGL session descriptions each frame.");

            var waitSource = File.ReadAllText(ResolveUnityWebRtcWrapperPath());
            Assert.That(waitSource, Does.Contain("Task.Yield()"),
                "CreateOfferAsync must poll with Task.Yield. Task.Delay uses a thread-pool timer and never resumes on WebGL.");
            Assert.That(waitSource, Does.Not.Contain("Task.Delay"),
                "WebRTC async wait must not use Task.Delay; it deadlocks CreateOfferAsync in the WebGL player.");
            Assert.That(waitSource, Does.Contain("WebGLSessionOps.PumpPendingSessionOps"),
                "CreateOfferAsync must pump pending SDP itself so join does not depend on coroutine order after createOffer resolves.");
        }

        [Test]
        public void When_media_device_panel_source_expect_dropdown_refresh_on_unknown_device()
        {
            var path = ResolveMediaDevicePanelPath();
            Assert.That(File.Exists(path), Is.True,
                $"Expected MediaDevicePanelBase at {path}.");

            var source = File.ReadAllText(path);
            var methodStart = source.IndexOf("public void SelectDeviceWithoutNotify", StringComparison.Ordinal);
            Assert.That(methodStart, Is.GreaterThanOrEqualTo(0),
                "MediaDevicePanelBase must expose SelectDeviceWithoutNotify.");
            var methodEnd = source.IndexOf("public void NotifyParentShow", methodStart, StringComparison.Ordinal);
            Assert.That(methodEnd, Is.GreaterThan(methodStart),
                "Could not bound SelectDeviceWithoutNotify for the dropdown-refresh check.");
            var method = source.Substring(methodStart, methodEnd - methodStart);
            Assert.That(method, Does.Contain("UpdateDevicesDropdown(GetDevices())"),
                "WebGL camera names change from 'Video input #1' to real names after getUserMedia. The dropdown must refresh before failing to find the selected device.");
        }

        static string ResolveMediaDevicePanelPath()
        {
            var candidates = new[]
            {
                Path.GetFullPath("Packages/io.getstream.video/Samples~/VideoChat/Scripts/UI/Devices/MediaDevicePanelBase.cs"),
                Path.GetFullPath("Packages/StreamVideo/Samples~/VideoChat/Scripts/UI/Devices/MediaDevicePanelBase.cs")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }

        static string ResolveUnityWebRtcWrapperPath()
        {
            var candidates = new[]
            {
                Path.GetFullPath("Packages/io.getstream.video/Runtime/Core/LowLevelClient/UnityWebRtcWrapperExtensions.cs"),
                Path.GetFullPath("Packages/StreamVideo/Runtime/Core/LowLevelClient/UnityWebRtcWrapperExtensions.cs")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }

        static string ResolveUnityWebRequestHttpClientPath()
        {
            var candidates = new[]
            {
                Path.GetFullPath("Packages/io.getstream.video/Runtime/Libs/Http/UnityWebRequestHttpClient.cs"),
                Path.GetFullPath("Packages/StreamVideo/Runtime/Libs/Http/UnityWebRequestHttpClient.cs")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
        }

        static string ResolveAudioDeviceManagerPath()
        {
            var candidates = new[]
            {
                Path.GetFullPath("Packages/io.getstream.video/Runtime/Core/DeviceManagers/StreamAudioDeviceManager.cs"),
                Path.GetFullPath("Packages/StreamVideo/Runtime/Core/DeviceManagers/StreamAudioDeviceManager.cs")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return candidates[0];
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
