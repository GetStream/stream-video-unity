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
        public void When_sfu_twirp_http_source_expect_no_thread_pool_content_read()
        {
            var handlerPath = ResolveUnityWebRequestHttpMessageHandlerPath();
            Assert.That(File.Exists(handlerPath), Is.True,
                $"Expected UnityWebRequestHttpMessageHandler at {handlerPath}.");
            var handler = File.ReadAllText(handlerPath);
            Assert.That(handler, Does.Not.Contain("ReadAsByteArrayAsync"),
                "WebGL has no thread pool. HttpContent.ReadAsByteArrayAsync uses ContinueWith(TaskScheduler.Default) and hangs SetPublisher after create-call.");
            Assert.That(handler, Does.Contain("HttpContentBytes.ReadAllAsync"),
                "SFU Twirp request bodies must be copied with CopyToAsync so ByteArrayContent completes on the main thread.");
            Assert.That(handler, Does.Contain("RequestTimeoutSeconds"),
                "UnityWebRequest default timeout is 0 (infinite). CORS/XHR stalls would leave create-call in Joining forever.");

            var generatedApi = File.ReadAllText(ResolveGeneratedApiPath());
            Assert.That(generatedApi, Does.Not.Contain("ReadAsByteArrayAsync"),
                "Twirp response bodies must not use ReadAsByteArrayAsync on WebGL.");
            Assert.That(generatedApi, Does.Contain("HttpContentBytes.ReadAllAsync"),
                "Twirp response parsing must use the WebGL-safe content reader.");
            Assert.That(generatedApi, Does.Contain("client.SendAsync("),
                "Twirp must call SendAsync so WebGLHttpClient can skip the thread-pool continuation. PostAsync may bypass the override.");
            Assert.That(generatedApi, Does.Not.Contain("PostAsync"),
                "HttpClient.PostAsync can skip WebGLHttpClient.SendAsync and hang SetPublisher after create-call.");

            var reader = File.ReadAllText(ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Libs/Http/HttpContentBytes.cs",
                "Packages/StreamVideo/Runtime/Libs/Http/HttpContentBytes.cs"));
            Assert.That(reader, Does.Contain("CopyToAsync"),
                "HttpContentBytes must copy via CopyToAsync. Awaiting an already-completed ByteArrayContent task stays on the Unity main thread.");
        }

        [Test]
        public void When_webgl_sfu_http_client_source_expect_sendasync_skips_thread_pool()
        {
            var clientPath = ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Libs/Http/WebGLHttpClient.cs",
                "Packages/StreamVideo/Runtime/Libs/Http/WebGLHttpClient.cs");
            Assert.That(File.Exists(clientPath), Is.True,
                $"Expected WebGLHttpClient at {clientPath}.");
            var client = File.ReadAllText(clientPath);
            Assert.That(client, Does.Contain("public override Task<HttpResponseMessage> SendAsync"),
                "WebGLHttpClient must override SendAsync. HttpClient.PostAsync uses ConfigureAwait(false) and hangs SetPublisher after create-call.");
            Assert.That(client, Does.Contain("_handler.Send("),
                "Override must send through UnityWebRequestHttpMessageHandler, not base.SendAsync.");
            Assert.That(client, Does.Not.Contain("base.SendAsync"),
                "base.SendAsync reintroduces ConfigureAwait(false) and hangs join in the WebGL player.");
            Assert.That(client, Does.Not.Contain("ConfigureAwait"),
                "WebGL SFU HTTP must stay on the Unity main thread.");
            Assert.That(client, Does.Contain("HttpRequestUriResolver.Resolve"),
                "WebGLHttpClient must resolve /twirp paths against BaseAddress. Mono/IL2CPP treats them as file:///twirp/... so IsAbsoluteUri skips the SFU host.");

            var handler = File.ReadAllText(ResolveUnityWebRequestHttpMessageHandlerPath());
            Assert.That(handler, Does.Contain("request.RequestUri.IsFile"),
                "UnityWebRequestHttpMessageHandler must reject file URIs. XHR of file:///twirp/... is blocked as a local resource in the WebGL player.");

            var lowLevel = File.ReadAllText(ResolveLowLevelClientPath());
            Assert.That(lowLevel, Does.Contain("new WebGLHttpClient()"),
                "CreateSessionHttpClient must use WebGLHttpClient. Plain HttpClient.PostAsync never resumes after UnityWebRequest yields.");
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
        public void When_webgl_media_ids_expect_browser_sdp_identities()
        {
            var streamJslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "MediaStream.jslib"));
            Assert.That(streamJslib, Does.Contain("stream.id || stream.guid"),
                "MediaStream.Id must return the browser stream.id. guid is a C# label and is not what SDP a=msid uses.");

            var trackJslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "MediaStreamTrack.jslib"));
            Assert.That(trackJslib, Does.Contain("track.id || track.guid"),
                "MediaStreamTrack.Id must return the browser track.id so SetPublisher matches SDP.");

            var transceiverJslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "RTCRtpTransceiver.jslib"));
            Assert.That(transceiverJslib, Does.Contain("getCapabilities"),
                "setCodecPreferences must map JSON codecs onto getCapabilities() objects. Chrome rejects reconstructed dictionaries with InvalidModification.");

            var videoTrackJslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "VideoStreamTrack.jslib"));
            Assert.That(videoTrackJslib, Does.Contain("texSubImage2D"),
                "Unity WebGL2 RenderTextures are immutable. texImage2D throws Texture is immutable.");

            var rendererJslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "VideoRenderer.jslib"));
            Assert.That(rendererJslib, Does.Contain("texSubImage2D"),
                "Remote video upload must use texSubImage2D on immutable Unity textures.");
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

        [Test]
        public void When_webgl_debug_log_source_expect_native_log_flag_respected()
        {
            var common = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "Common.jslib"));
            Assert.That(common, Does.Contain("if (!enableNativeLog)"),
                "RegisterDebugLog must ignore Info-level jslib logs unless native logging is enabled. Development WebGL dumps a stack for every ICE candidate.");
            Assert.That(common, Does.Contain("uwcom_dynCall('vii', uwevt_DebugLog"),
                "jslib debug logs must use uwcom_dynCall. Module.dynCall_vii is missing on some Unity WebGL players.");
        }

        [Test]
        public void When_webgl_ice_candidate_source_expect_intptr_not_string()
        {
            var webrtc = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Scripts", "WebRTC.cs"));
            Assert.That(webrtc, Does.Contain("IntPtr ptr, IntPtr iceCandidatePtr, IntPtr candidate, IntPtr sdpMid"),
                "WebGL ICE callbacks must take IntPtr. Reverse P/Invoke string marshalling from onicecandidate drops trickle ICE so remote video never starts.");

            var peerCs = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Scripts", "RTCPeerConnection.cs"));
            Assert.That(peerCs, Does.Contain("Marshal.PtrToStringAnsi(sdpPtr)"),
                "C# must copy ICE candidate strings from JS heap pointers.");
        }

        [Test]
        public void When_webgl_ontrack_source_expect_not_metadata_only()
        {
            var jslib = File.ReadAllText(Path.Combine(ResolveWebRtcRuntimeDir(), "Plugins", "WebGL", "RTCPeerConnection.jslib"));
            Assert.That(jslib, Does.Contain("setTimeout(fireOnTrack, 1000)"),
                "Remote ontrack must not wait only for onloadedmetadata. If metadata never fires, C# never binds the remote video texture.");
        }

        [Test]
        public void When_video_device_manager_source_expect_webcam_play_only_when_enabled()
        {
            var path = ResolveVideoDeviceManagerPath();
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("Lobby preview needs the webcam running"),
                "Playing the webcam while the publisher track is disabled makes lobby preview look enabled, then join publishes muted.");
            Assert.That(source, Does.Contain("if (enable && _activeCamera != null && !_activeCamera.isPlaying)"),
                "SelectDevice must start capture only when enable is true so lobby camera state is the publisher state.");
        }

        [Test]
        public void When_sample_ui_source_expect_select_device_preserves_enabled()
        {
            var ui = File.ReadAllText(ResolveSampleUiManagerPath());
            Assert.That(ui, Does.Contain("var enable = _videoManager.Client.VideoDeviceManager.IsEnabled"),
                "Selecting the default camera after a gesture must not force enable: false. That overwrites a lobby camera toggle.");

            var cameraPanel = File.ReadAllText(ResolveCameraMediaDevicePanelPath());
            Assert.That(cameraPanel, Does.Contain("SyncDeviceButton(isEnabled)"),
                "IsEnabledChanged must only update the sprite. UpdateDeviceState calls SetEnabled and races with SelectDevice.");
        }

        [Test]
        public void When_native_websocket_jslib_source_expect_dyncall_fallback()
        {
            var jslib = File.ReadAllText(ResolveNativeWebSocketJslibPath());
            Assert.That(jslib, Does.Contain("$ws_dynCall"),
                "NativeWebSocket must use a dynCall fallback. Module.dynCall_viii is missing on some Unity WebGL players, which drops SFU events after join.");
            Assert.That(jslib, Does.Not.Contain("Module.dynCall_viii"),
                "SFU protobuf frames must not use Module.dynCall_viii directly.");
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

        static string ResolveLowLevelClientPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Core/LowLevelClient/StreamVideoLowLevelClient.cs",
                "Packages/StreamVideo/Runtime/Core/LowLevelClient/StreamVideoLowLevelClient.cs");

        static string ResolveUnityWebRequestHttpClientPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Libs/Http/UnityWebRequestHttpClient.cs",
                "Packages/StreamVideo/Runtime/Libs/Http/UnityWebRequestHttpClient.cs");

        static string ResolveUnityWebRequestHttpMessageHandlerPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Libs/Http/UnityWebRequestHttpMessageHandler.cs",
                "Packages/StreamVideo/Runtime/Libs/Http/UnityWebRequestHttpMessageHandler.cs");

        static string ResolveGeneratedApiPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Core/InternalDTO/Sfu/GeneratedAPI.cs",
                "Packages/StreamVideo/Runtime/Core/InternalDTO/Sfu/GeneratedAPI.cs");

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

        static string ResolveVideoDeviceManagerPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Core/DeviceManagers/StreamVideoDeviceManager.cs",
                "Packages/StreamVideo/Runtime/Core/DeviceManagers/StreamVideoDeviceManager.cs");

        static string ResolveSampleUiManagerPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Samples~/VideoChat/Scripts/UI/UIManager.cs",
                "Packages/StreamVideo/Samples~/VideoChat/Scripts/UI/UIManager.cs");

        static string ResolveCameraMediaDevicePanelPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Samples~/VideoChat/Scripts/UI/Devices/CameraMediaDevicePanel.cs",
                "Packages/StreamVideo/Samples~/VideoChat/Scripts/UI/Devices/CameraMediaDevicePanel.cs");

        static string ResolveNativeWebSocketJslibPath()
            => ResolveExistingPath(
                "Packages/io.getstream.video/Runtime/Libs/NativeWebSocket/WebSocket.jslib",
                "Packages/StreamVideo/Runtime/Libs/NativeWebSocket/WebSocket.jslib");

        static string ResolveExistingPath(params string[] relativeCandidates)
        {
            foreach (var relative in relativeCandidates)
            {
                var path = Path.GetFullPath(relative);
                if (File.Exists(path))
                    return path;
            }

            return Path.GetFullPath(relativeCandidates[0]);
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
