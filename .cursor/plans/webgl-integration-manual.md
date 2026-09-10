# WebGL Unity.WebRTC Integration Manual

How this branch delivered browser WebRTC under the existing Stream Video C# API, written so you can attempt the same integration yourself.

**Branch:** `feature/prototype_add_webgl_support`  
**Commits:** `2fb0249 Add WebGL support`, `f618ec4 temp`  
**Status:** prototype. Experimental. Not a second SDK.

Companion (LiveKit research, not this delivery): `.cursor/plans/livekit-unity-webgl-webrtc-analysis.md`

How to read this:

1. Read Parts 1–4 before writing any `.jslib`. The architecture and plugin mechanics are the whole trick.
2. Then walk Part 5 in order. Each step is a real layer this branch added, with the file you should open.
3. Part 6 is the failure catalog. Treat those as unit tests for your own attempt.
4. Editor Play Mode is **not** WebGL. Every `#if UNITY_WEBGL && !UNITY_EDITOR` is intentional.

---

## Part 1 — The one decision you must not reverse

Do **not** wrap a JavaScript video SDK (`stream-video-js`, LiveKit `livekit-client`, etc.) and expose a second C# API.

Customers already call `IStreamVideoClient` / `IStreamCall`. Signaling, SFU websocket, protobuf, `RtcSession`, `PublisherPeerConnection`, and `SubscriberPeerConnection` stay in C# on every platform.

WebGL only substitutes the **Unity.WebRTC native backend**:

```
Customer C#  (IStreamCall, tracks, devices)     ← unchanged
        │
RtcSession / PublisherPC / SubscriberPC / SFU WS ← unchanged
        │
Unity.WebRTC C#  (RTCPeerConnection, VideoStreamTrack, …)
        │
        ├─ native player: webrtc.dll / .so / .a     DllImport "webrtc"
        └─ WebGL player:  Plugins/WebGL/*.jslib     DllImport "__Internal"
                          → browser RTCPeerConnection / MediaStream / WebGL
```

LiveKit shipped two Unity packages with two public APIs. That is the wrong product split for this repo. Copy their **browser physics** (GPU copy, hidden `<video>`/`<audio>`, autoplay unlock, `.jslib` / `.jspre`), not their product architecture.

If you find yourself designing a C# `HTMLVideoElement` or a JS `Room` remote-control layer, stop. You are building a second SDK.

---

## Part 2 — Why this is hard

Unity WebGL compiles C# → IL2CPP → WASM. There is no `webrtc.dll`. There is no libwebrtc. There is no thread pool that resumes `Task.Delay`. `HttpClient` has no working socket handler. `UnityEngine.Microphone` is not in the WebGL player API.

The browser **does** have WebRTC (`RTCPeerConnection`, `getUserMedia`, `HTMLVideoElement`, `captureStream`). Your job is to implement the existing C# `NativeMethods` ABI against those browser objects.

Three physics problems dominate:

1. **Identity.** C# holds `IntPtr`. JS holds objects. You must invent a handle table.
2. **Async.** Browser APIs return Promises. Native Unity.WebRTC returns observer objects completed from C++. Crossing Promise → IL2CPP reverse P/Invoke with marshaled strings hangs the player. You must not do that.
3. **Pixels and samples.** Decoded video is an `HTMLVideoElement`, not a Unity texture. Local Unity textures are WebGL textures. Audio is a browser `AudioContext` / `<audio>` element, not `OnAudioFilterRead` into libwebrtc.

Everything else (HTTP CORS, websocket handshake, permissions, autoplay) is the surrounding browser sandbox. Ignore it and join will fail before a peer connection exists.

---

## Part 3 — Inventory the ABI you must implement

Open these before writing JS:

| Layer | Path | What it is |
|---|---|---|
| Public C# | `Packages/StreamVideo/Runtime/Core/` | Customer API. Touch only where a Unity type is missing on WebGL. |
| Unity.WebRTC C# | `.../io.stream.unity.webrtc/Runtime/Scripts/` | The contract. `RTCPeerConnection`, `VideoStreamTrack`, `NativeMethods`. |
| NativeMethods | `WebRTC.cs` (`DllImport`) | The ABI. Native: `Lib = "webrtc"`. WebGL: `Lib = "__Internal"`. |
| WebGL plugins | `.../Runtime/Plugins/WebGL/*.jslib` | Your implementation of that ABI. |
| webrtc-adapter | `adapter.jspre` | Browser shim. Runs **before** the player. |

The C# types already exist. On native they call into C++. On WebGL the **same method names** must exist as Emscripten library functions.

Do not invent a generic JS FFI (`CallMethod("createOffer")`). That is LiveKit’s approach for wrapping a JS SDK. You already have a fixed native ABI. Implement it function-for-function.

Useful grep: every `[DllImport(WebRTC.Lib)]` in `WebRTC.cs` is a function you either implement in jslib or stub.

---

## Part 4 — Unity WebGL plugin mechanics

Learn this once. Both this branch and LiveKit depend on it.

### 4.1 File types

| Extension | When it runs | Role |
|---|---|---|
| `.jspre` | Concatenated **before** the compiled player JS | Polyfills / large libraries. Here: `adapter.jspre` (webrtc-adapter). |
| `.jslib` | Merged into Emscripten `LibraryManager.library` at link | Functions C# can `DllImport("__Internal")`. Must end with `mergeInto(LibraryManager.library, YourLib)`. |

`.meta` must enable **WebGL only** (`Exclude WebGL: 0`, other platforms excluded) and typically `isPreloaded: 1`. Copy an existing `.jslib.meta` rather than inventing one.

Editor is not WebGL. Plugins do not run in Play Mode.

### 4.2 C# import split

```csharp
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
    internal const string Lib = "__Internal";
#else
    internal const string Lib = "webrtc";
#endif
```

iOS also uses `__Internal` (static lib). WebGL shares that string but not the implementation.

### 4.3 `$`-prefixed state and `autoAddDeps`

Emscripten only preserves library properties that start with `$`, and only if you declare them as dependencies:

```javascript
var UnityWebRTCCommon = {
  $UWManaged: {},
  $uwcom_addManageObj: function (obj) { ... },
  SomeExportedFn: function () { ... }
};
autoAddDeps(UnityWebRTCCommon, '$UWManaged');
autoAddDeps(UnityWebRTCCommon, '$uwcom_addManageObj');
mergeInto(LibraryManager.library, UnityWebRTCCommon);
```

Without `autoAddDeps`, `$UWManaged` is stripped and every call sees a missing table. This is the most common silent jslib bug.

Cross-file calls: `ContextCreatePeerConnection__deps: ['CreatePeerConnection']` then `_CreatePeerConnection(conf)`. The `_` prefix is the Emscripten-minified export of another library function.

### 4.4 Handle table

C# cannot hold a JS object. Pattern used here:

```javascript
$UWManaged: {},          // managePtr → JS object
$uwcom_managePtr: 0,

$uwcom_addManageObj: function (obj) {
  if (!obj.managePtr) {
    uwcom_managePtr++;
    obj.managePtr = uwcom_managePtr;
    UWManaged[obj.managePtr] = obj;
  }
}
```

C# `IntPtr` **is** `managePtr`. Lookup: `UWManaged[ptr]`. Delete on dispose. Always `uwcom_existsCheck(ptr, funcName, typeName)` before use.

This is **not** a refcounted identity map (LiveKit’s `GetOrNewRef`). Same JS object seen twice can get two ptrs unless you reuse `obj.managePtr`. That is acceptable for a 1:1 C# wrapper, dangerous if JS returns the same transceiver from two APIs. Prefer attaching `managePtr` on the object itself so later lookups reuse it.

### 4.5 Strings and arrays across the WASM heap

JS → C#:

```javascript
$uwcom_strToPtr: function (str) {
  if (str == null) str = '';
  var len = lengthBytesUTF8(str) + 1;
  var ptr = _malloc(len);
  stringToUTF8(str, ptr, len);
  return ptr;
}
```

C# reads with `Marshal.PtrToStringAnsi` / `[MarshalAs(UnmanagedType.LPStr)]`. Who `_free`s must be defined. This branch often frees next tick:

```javascript
setTimeout(function () { _free(ptr); }, 0);
```

That is safe only if C# copies during the same turn. Do not return a pointer and expect it to live across `await`.

Arrays of handles use a **length prefix**:

```javascript
$uwcom_arrayToReturnPtr: function (arr, type) {
  var ui8a = new Uint8Array((new type(arr)).buffer);
  var ptr = _malloc(ui8a.byteLength + 4);
  HEAP32.set([arr.length], ptr >> 2);
  HEAPU8.set(ui8a, ptr + 4);
  setTimeout(function () { _free(ptr); }, 0);
  return ptr;
}
```

C# counterpart: `WebGLSessionOps.PtrToIntPtrArray` (`ReadInt32` length, then copy ints). Native Unity.WebRTC returns `(ptr, length)` as two out-params. WebGL has one return value, so the length lives in the buffer. That is why `GetSenders` / `GetReceivers` / `GetTransceivers` / `AddTrack` are `#if UNITY_WEBGL` in `RTCPeerConnection.cs`.

C# → JS: `UTF8ToString(ptr)`, `HEAPU8.subarray`, `HEAPF32.subarray` (audio).

### 4.6 JS → C# callbacks (`dynCall`)

C# registers a static method marked `[AOT.MonoPInvokeCallback(typeof(TheDelegate))]`. JS receives a function pointer and invokes it:

Signature letters: `v` void, `i` int/pointer.

```javascript
Module.dynCall_vii(fnPtr, arg0, arg1);   // void(int, int)
```

Unity versions differ. This branch wraps fallbacks in `$uwcom_dynCall(sig, funcPtr, ...args)`:

1. `Module['dynCall_' + sig]`
2. `Module.dynCall(sig, fn, args)`
3. `wasmTable.get(fn)`

Keep that helper. Direct `Module.dynCall_vii` will break on some Unity versions.

**Never** `dynCall` from a Promise continuation with marshaled `string` arguments into IL2CPP. That is the CreateOffer hang (Part 5.6). Events like `onicecandidate` that fire from the browser event loop with integer handles are OK.

### 4.7 `GL.textures` and `GLctx`

Emscripten maps Unity texture native IDs to WebGLTexture objects:

```javascript
GL.textures[id]   // WebGLTexture
GLctx             // Unity's WebGLRenderingContext
```

`texture.GetNativeTexturePtr()` on C# is that `id`. JS looks it up and `texImage2D` / `readPixels` against it.

Two ownership models (both valid):

| Model | Who creates the GL texture | How Unity binds it |
|---|---|---|
| LiveKit receive | JS `GLctx.createTexture()` + `GL.getNewId` | `Texture2D.CreateExternalTexture(..., (IntPtr)id)` |
| This branch receive | Unity `new Texture2D` then `GetNativeTexturePtr()` | JS `texImage2D` into `GL.textures[ptr]` |

This branch’s `CreateNativeTexture` scans `GL.textures` for the first `undefined` slot. Prefer `GL.getNewId(GL.textures)` if you ever switch to JS-owned textures. The receive path currently uses Unity-created `Texture2D`, so the scanner is unused in the hot path.

---

## Part 5 — Implementation recipe (do it in this order)

This is the order that matches how the stack actually boots: platform → ABI → peer → SDP → media → then the SDK sandbox (HTTP, WS, devices).

### Step 1 — Enable the platform so the package even compiles

Files:

- `Stream.Unity.WebRTC.Runtime.asmdef` — add `"WebGL"` to `includePlatforms`. Without this the assembly is stripped from WebGL player builds.
- `io.stream.unity.webrtc/package.json` — document experimental WebGL; add `com.unity.nuget.newtonsoft-json` if C# JSON DTOs need it (this branch did).
- `Packages/StreamVideo/package.json` — mention experimental WebGL in the customer description.
- Internals: `[InternalsVisibleTo("StreamVideo.Core")]` on the WebRTC assembly so the SDK can call `WebGLSessionOps.PumpPendingSessionOps`.

Do this first. A missing platform in the asmdef looks like “WebRTC types don’t exist” and wastes a day.

### Step 2 — Point `DllImport` at `__Internal` and stub the ABI

In `WebRTC.cs`, `Lib` becomes `"__Internal"` on WebGL player.

Create `Plugins/WebGL/` with one jslib per native module, mirroring C++:

| jslib | NativeMethods cluster |
|---|---|
| `Common.jslib` | handle table, strings, dynCall, autoplay, track lifetime |
| `Enum.jslib` | C# enum ordinals ↔ browser strings |
| `Context.jslib` | `ContextCreate`, peer/track/stream factories |
| `RTCPeerConnection.jslib` | offer/answer, ICE, transceivers, ontrack |
| `RTCIceCandidate.jslib` | candidate fields |
| `RTCRtpSender/Receiver/Transceiver.jslib` | replaceTrack, params, direction |
| `MediaStream.jslib` / `MediaStreamTrack.jslib` | add/remove/enabled |
| `VideoStreamTrack.jslib` | local canvas capture |
| `VideoRenderer.jslib` | remote `texImage2D` |
| `AudioStreamTrack.jslib` | AudioContext destination + PCM inject |
| `RTCDataChannel.jslib` | send/receive |
| `adapter.jspre` | webrtc-adapter |

Stub every `NativeMethods` entry even if the body is empty. Missing exports fail at **link**, not at runtime. Return `0` / empty string for unused features (`GetStats` throws `NotSupportedException` in C# on this prototype).

`Enum.jslib` is not decoration. C# sends `int` for `RTCSdpType`, `RTCIceConnectionState`, etc. JS must use the **same ordinals** as the C# enum. Keep arrays aligned with `Unity.WebRTC` enum declarations. `indexOf` string → int, `[idx]` int → string.

`adapter.jspre` is the webrtc-adapter IIFE. It shims Safari/Firefox differences (`getUserMedia`, `srcObject`, transceiver helpers). Drop it in as a `.jspre`; do not rewrite it.

### Step 3 — Context and peer construction

`Context.jslib`:

- `ContextCreate` allocates a JS `{ id, refPtr: Set }` and returns `managePtr`.
- `SetCurrentContext` stores it on `UWManaged["__currentContext__"]`.
- `ContextCreatePeerConnectionWithConfig` JSON.parses C# config, maps optional-value wrappers (`iceTransportPolicy.hasValue`) to real strings, then `new RTCPeerConnection(conf)`.

`RTCPeerConnection.jslib` `CreatePeerConnection` must wire events **at construction**, not later:

- `onicecandidate` → add handle, `dynCall('viiiii', …)`
- `oniceconnectionstatechange` / `onconnectionstatechange` / `onicegatheringstatechange` / `onnegotiationneeded`
- `ondatachannel`
- `ontrack` — this is also where you attach hidden media elements (Step 9)

Register C# callbacks once at `WebRTC.InitializeInternal`:

```csharp
NativeMethods.RegisterDebugLog(...);
NativeMethods.WebGLRegisterCreateSessionCallbacks(success, failure);
NativeMethods.WebGLRegisterSetSessionCallbacks(success, failure);
```

JS stores those pointers in `$uwevt_*`. Native Unity.WebRTC registers per-peer observers in C++. WebGL uses process-wide function pointers plus a per-peer queue in C# (`WebGLSessionOps`).

### Step 4 — C# `#if` only where the native ABI cannot be preserved

Keep `#if UNITY_WEBGL && !UNITY_EDITOR` **inside** Unity.WebRTC, not in `IStreamCall`.

Legitimate reasons this branch used it:

| Reason | Example |
|---|---|
| Return shape differs (length-prefixed buffer vs out-length) | `GetSenders` |
| Extra handle in a callback | `PCOnIceCandidate(..., IntPtr iceCandidatePtr, ...)` |
| JSON instead of blittable struct | `CreateOffer(ptr, JsonUtility.ToJson(options))` |
| Feature stub | `GetStats` → `NotSupportedException` |
| Observer is C#-allocated, not native-allocated | `CreateWithHandle` + enqueue |

If you can keep the C# signature and only change the jslib, do that.

JSON DTOs live in `WebGLSessionOps.cs` (`WebGLSessionDescriptionDto`, `WebGLRtpCapabilitiesDto`, …) because `JsonUtility` needs `[Serializable]` classes. `WebGLRtpCapabilitiesParser` converts browser `getCapabilities` JSON into `RTCRtpCodecCapability`.

### Step 5 — ICE

Browser `onicecandidate` gives an `RTCIceCandidate`. Add it to `UWManaged`, pass `candidate` / `sdpMid` / `sdpMLineIndex` to C#.

`addIceCandidate` before `setRemoteDescription` fails in browsers. Queue on the JS peer:

```javascript
if (!peer.remoteDescription) {
  peer._pendingIceCandidates = peer._pendingIceCandidates || [];
  peer._pendingIceCandidates.push(candidate);
  return true;
}
```

Flush that queue when `setRemoteDescription` resolves. This is required, not optional.

C# `RTCIceCandidate` on WebGL is constructed with the JS handle so later `addIceCandidate` can look it up. Native constructs from SDP strings only.

### Step 6 — Session descriptions without hanging the player (the hard lesson)

Native path:

1. C++ `CreateOffer` returns a `CreateSessionDescriptionObserver*` (real native handle).
2. C++ completes it later; C# callback fires.

WebGL path you might try first (and must not ship):

1. `peer.createOffer().then(offer => dynCall(success, peer, type, sdpString))`
2. IL2CPP reverse P/Invoke with `string` from a Promise.

That **hangs join**. Confirmed on this branch. Root cause: Promise → `dynCall` into IL2CPP with marshaled strings is unreliable. Tests encode the ban:

- Do **not** `uwcom_dynCall('viii', uwevt_OnSuccessCreateSessionDesc, …)` from createOffer’s `.then`.
- Do **not** `uwcom_dynCall('vi', uwevt_OnSetSessionDescSuccess, …)` from setLocal/RemoteDescription’s `.then`.
- Callbacks that *are* registered must take `IntPtr`, not `string`.

What this branch does instead:

**JS** stores the result on the peer object:

```javascript
peer.createOffer(options).then(function (offer) {
  peer._pendingCreateSd = { type: 0, sdp: offer.sdp || '', errorType: 0, message: '' };
}).catch(function (err) {
  peer._pendingCreateSd = { type: 0, sdp: '', errorType: uwcom_errorNo(err), message: err.message || '' };
});
```

Same for createAnswer (`type: 2`) and setLocal/setRemote (`_pendingSetSd`).

**JS** exports takers:

```javascript
PeerConnectionTakePendingCreateSd: function (peerPtr) {
  var peer = UWManaged[peerPtr];
  if (!peer || !peer._pendingCreateSd) return uwcom_strToPtr('');
  var json = JSON.stringify(peer._pendingCreateSd);
  peer._pendingCreateSd = null;
  return uwcom_strToPtr(json);
}
```

**C#** `WebGLSessionOps`:

1. `EnqueueCreate(peer)` allocates an observer with a **synthetic** `IntPtr` (`Interlocked.Increment`). Native observers are real C++ pointers; on WebGL there is no C++ object, so C# mints the handle (`CreateWithHandle`).
2. `WebRTC.Update` (and `CreateOfferAsync`) calls `PumpPendingSessionOps`.
3. Pump reads JSON via `PeerConnectionTakePendingCreateSd`, `JsonUtility.FromJson<WebGLPendingSessionOpDto>`, dequeues the observer, `observer.Invoke(...)`.

**C# wait loop** (`UnityWebRtcWrapperExtensions`):

```csharp
while (!asyncOperation.IsDone)
{
#if UNITY_WEBGL && !UNITY_EDITOR
    WebGLSessionOps.PumpPendingSessionOps();
#endif
    await Task.Yield();   // NOT Task.Delay
}
```

`Task.Delay` uses a thread-pool timer. There is no thread pool that resumes it on WebGL IL2CPP. The await never continues. `Task.Yield` returns to the Unity player loop.

Pump from **both** `WebRTC.Update` and the wait loop. Join must not depend on coroutine order after createOffer resolves.

`setLocalDescription` / `setRemoteDescription` return immediately with `RTCErrorType.None` from JS (the Promise is still in flight). Completion is the pump. Callers already wait on `SetSessionDescriptionObserver` / `RTCSessionDescriptionAsyncOperation`, so this preserves the C# API.

### Step 7 — Transceivers, senders, receivers

`addTrack` / `addTransceiver` return JS objects; `uwcom_addManageObj` then return `managePtr`.

C# `AddTrack` on native: `(error, senderPtr)` out-params. On WebGL: length-prefixed `[error, senderPtr]`.

Codec preferences and send parameters go through JSON (`WebGLRtpCapabilitiesParser.ToCodecPreferencesJson` / `ToSendParametersJson`) because you cannot blit a C# struct into a browser dictionary.

`replaceTrack` is a real browser API — implement it. Mute/unmute and camera switch depend on it.

### Step 8 — Local video (Unity texture → MediaStreamTrack)

Browsers cannot encode a Unity `Texture` directly. They can encode a `<canvas>` via `captureStream()`.

`VideoStreamTrack` ctor on WebGL (`CreateVideoTrack`):

1. Create a destination `RenderTexture`.
2. Pass `dest.GetNativeTexturePtr()` as both src and dst into JS (this prototype uses the dest RT as the GPU source after Unity’s copy/flip).
3. JS creates a hidden canvas, `captureStream()`, keeps the `MediaStreamTrack`, stores `{ cnv, ctx, imgData, frameBuffer, dstTexture, … }` in `$uwcom_localVideoTracks[trackPtr]`.

Each frame (`VideoStreamTrack.UpdateTexture` → `RenderLocalVideotrack`):

1. Bind an FBO to the Unity texture (`framebufferTexture2D`).
2. `readPixels` into a `Uint8Array`.
3. `putImageData` onto the canvas (that is what `captureStream` samples).
4. Optionally Y-flip with a canvas `scale(1,-1)` blit.
5. `texImage2D` back into the dest Unity texture so local preview matches what is sent.

Cost: a GPU readback every frame. That is the current prototype. There is an unfinished `readPixelsAsync` / `PIXEL_PACK_BUFFER` path in the same file — do not enable it until it is complete.

`WebRTC.Update` must keep running. No update, no pixels on the canvas, no video on the wire.

### Step 9 — Remote video (MediaStreamTrack → Unity texture)

`ontrack` for video:

1. Wrap the track in a **new** `MediaStream` (this branch also `removeTrack` from `evt.streams[0]` — isolate ownership).
2. Create a hidden `<video srcObject=stream muted playsinline>`.
3. `uwcom_attachHiddenMediaElement` (1×1, offscreen, `play()`).
4. Store in `$uwcom_remoteVideoTracks[trackPtr]`.
5. Fire `PCOnTrack` on `onloadedmetadata` so C# does not bind a 0×0 texture.

Hidden DOM elements are required. `texImage2D(..., video)` needs a decoding element. Unity cannot see the browser decoder output any other way.

Each frame: `UpdateRendererTexture(trackPtr, unityTexturePtr, needFlip)`:

```javascript
GLctx.bindTexture(GLctx.TEXTURE_2D, GL.textures[renderTexturePtr]);
GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
GLctx.texImage2D(GLctx.TEXTURE_2D, 0, GLctx.RGBA, GLctx.RGBA, GLctx.UNSIGNED_BYTE, video);
GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, false);
```

Y-flip is almost always needed (WebGL vs Unity texture origin).

C# receive `VideoStreamTrack` still creates a `Texture2D` / renderer. WebGL skips the native renderer update and calls `UpdateRendererTexture` instead.

Audio `ontrack`: hidden `<audio srcObject=stream>`, store in `$uwcom_remoteAudioTracks`. The **browser** plays it. Do not try to pull PCM into Unity `AudioSource` for v1. Autoplay will block this; see Step 14.

### Step 10 — Local audio

`UnityEngine.Microphone` is **not compiled** into the WebGL player. Any `using` of `Microphone.devices` / `Microphone.Start` fails the player compile. Guard every call with `#if UNITY_WEBGL && !UNITY_EDITOR`.

This prototype:

- Enumerates a dummy `"Default Microphone"`.
- Does **not** start `getUserMedia` audio yet (`StreamTODO` in `StreamAudioDeviceManager`).
- `CreateAudioTrack` makes an `AudioContext.createMediaStreamDestination()` and returns that track.
- `AudioSourceProcessLocalAudio` copies `HEAPF32` into an `AudioBuffer` and plays it into the destination.

That is enough to have a track object. It is **not** enough for real mic capture in the browser. When you implement it: `navigator.mediaDevices.getUserMedia({ audio: true })` after a user gesture, then `replaceTrack` / construct `AudioStreamTrack` from that `MediaStreamTrack`. Do not call `Microphone.Start`.

### Step 11 — HTTP in the browser sandbox

Two HTTP stacks exist in the SDK:

| Client | Used for | WebGL implementation |
|---|---|---|
| `IHttpClient` | coordinator REST, location hint, token | `UnityWebRequestHttpClient` (`StreamDependenciesFactory` already picked this on `UNITY_WEBGL`) |
| `System.Net.Http.HttpClient` | SFU Twirp | `new HttpClient(new UnityWebRequestHttpMessageHandler())` |

Native `HttpClientHandler` does not work on WebGL (no sockets). Twirp would die on first RPC without the message handler.

`UnityWebRequestHttpClient` rules this branch learned the hard way (encoded in `WebGLSupportTests`):

1. **Content-Type `application/json`.** Default Unity upload is `application/octet-stream`. Browsers CORS-fail it as `Unknown Error`.
2. **Timeout ≥ 60s.** CORS preflight + POST share one timer. 5s surfaces as `Unknown Error`.
3. **No body on GET/HEAD/DELETE.** Browsers reject GET bodies; Unity reports `ConnectionError`.
4. **Skip forbidden request headers.** Setting `User-Agent` throws. Catch `InvalidOperationException` per header.
5. **Location hint HEAD must not send Authorization.** Those headers trigger a preflight the hint CDN rejects. `includeDefaultHeaders: false` on HEAD. If the header is missing, **do not throw** — fall back to `"ERR"` (`StreamVideoLowLevelClient`). Cross-origin HEAD often cannot expose `x-amz-cf-pop`.

`UnityWebRequestHttpMessageHandler` is a thin `HttpMessageHandler`: copy method/URI/body/headers onto `UnityWebRequest`, `await Task.Yield` until done, wrap status + bytes as `HttpResponseMessage`. Swallow illegal header sets.

### Step 12 — WebSockets

`StreamDependenciesFactory` already used `NativeWebSocketWrapper` on `UNITY_WEBGL`.

NativeWebSocket’s WebGL `Connect()` returns when the JS call is made, **before** the browser handshake finishes. Coordinator auth then sends on a still-connecting socket.

`WebsocketConnectGate.WaitUntilOpenAsync` waits for `OnOpen`, or fails on error/close/cancel. Wire it in `NativeWebSocketWrapper.Connect`.

Also: `SendQueueCount` / `ClearSendQueue` cannot throw `NotImplementedException` on a path the SDK actually hits.

### Step 13 — Device enumeration and Unity types that do not exist

| API | WebGL player | This branch |
|---|---|---|
| `Microphone` | missing | dummy device; no recording |
| `WebCamTexture` | present, but `getUserMedia` needs a gesture | dummy `"Default Camera"` if `devices` is empty; always `Play()` in lobby so preview works before the publisher track is enabled |
| `WebCamTexture.GetPixels` | never completes without gesture + getUserMedia | `OnTestDeviceAsync` returns `true` without reading pixels |

After `getUserMedia`, browser camera **names change** (`"Video input #1"` → `"FaceTime HD"`). `MediaDevicePanelBase.SelectDeviceWithoutNotify` must `UpdateDevicesDropdown(GetDevices())` before looking up the selected name, or the UI thinks the device vanished.

### Step 14 — Permissions, autoplay, user gesture

Browsers require a **user gesture** for camera/mic and often for `AudioContext.resume` / `video.play()`.

Sample app (`UIManager`):

- Native: request devices in `Awake`.
- WebGL: wait for first click/touch in `Update`, then `TryRequestMediaDevices`.
- Log `"Click the page to allow camera and microphone access."`

`PermissionsManager`: camera uses `Application.RequestUserAuthorization(UserAuthorization.WebCam)` like iOS. Microphone `HasPermission` returns `true` because `Microphone` APIs are absent; real mic permission will come from `getUserMedia`.

JS side (`Common.jslib`):

- `$uwcom_attachHiddenMediaElement` — `autoplay`, `playsInline`, offscreen, `play().catch(() => {})`.
- `$uwcom_ensureAutoplayUnlock` — one-shot `pointerdown` / `keydown` listener that `audioContext.resume()` and retries all remote `<audio>`/`<video>` `.play()`.
- Call `uwcom_ensureAutoplayUnlock()` when creating the AudioContext and on `ontrack`.

Without this, remote audio is silent until a click, and Safari may never start video decode.

Dispose: `$uwcom_releaseMediaTrack` removes hidden elements, `track.stop()`, deletes table entries. Leaking `<video>` elements will keep cameras/mics captured after leave.

---

## Part 6 — Gotcha catalog (write tests that freeze these)

`Packages/StreamVideo/Tests/Editor/WebGLSupportTests.cs` and `WebsocketConnectGateTests.cs` are source-level guards. Recreate them if you reimplement. They catch regressions without a browser.

| Failure | Symptom | Fix |
|---|---|---|
| asmdef missing `"WebGL"` | package missing in player | include platform |
| `Task.Delay` in CreateOffer wait | join hangs after offer | `Task.Yield` |
| `dynCall` from Promise with strings | join hangs after offer | store SDP on JS peer; pump from Update |
| CreateOffer callback takes `string` | IL2CPP reverse P/Invoke hang | `IntPtr` + marshal in C# |
| Wait loop does not pump | offer never completes if Update order is wrong | pump in `CreateOfferAsync` too |
| GET with JSON body | `Unknown Error` | no body on GET/HEAD/DELETE |
| Content-Type octet-stream | CORS `Unknown Error` | `application/json` |
| 5s HTTP timeout | CORS `Unknown Error` | 60s |
| Location hint requires header | connect throws | fallback `"ERR"`; HEAD without auth |
| `HttpClient` sockets on Twirp | SFU RPC dead | `UnityWebRequestHttpMessageHandler` |
| `Connect()` returns early | auth send on connecting WS | `WebsocketConnectGate` |
| `Microphone.*` in player | compile fail | `#if UNITY_WEBGL && !UNITY_EDITOR` |
| Camera dropdown stale names | UI cannot select device after getUserMedia | refresh dropdown |
| Request devices in `Awake` | permission prompt never appears / denied | wait for click |
| `addIceCandidate` before remote SDP | ICE errors | queue until setRemoteDescription |
| No hidden `<video>` | black remote video | attach + `texImage2D` from element |
| No autoplay unlock | silent remote audio | gesture → `resume` + `play` |
| `Task.Delay` anywhere on WebGL hot path | never resumes | `Task.Yield` or Unity coroutine |

Editor tests cannot prove pixels or ICE. They **can** prove you did not reintroduce the hang and CORS mistakes.

---

## Part 7 — How a call actually flows on WebGL

Use this as a debugger map.

1. **Gesture.** User clicks. Sample requests webcam authorization. `WebCamTexture.Play()` starts (Unity’s getUserMedia).
2. **HTTP.** Token / coordinator via `UnityWebRequestHttpClient`. Location hint HEAD may fail; fallback `"ERR"`.
3. **Coordinator WS.** `NativeWebSocketWrapper.Connect` → gate waits `OnOpen` → auth frame sent.
4. **Join.** C# `RtcSession` creates publisher/subscriber `RTCPeerConnection` (JS `new RTCPeerConnection`).
5. **Local video.** `VideoStreamTrack(texture)` → hidden canvas `captureStream` → `addTrack` / transceiver.
6. **CreateOffer.** JS `createOffer` → `_pendingCreateSd` → C# pump → `SetLocalDescription` → `_pendingSetSd` → pump.
7. **SFU.** Offer posted through Twirp (`UnityWebRequestHttpMessageHandler`). Answer `setRemoteDescription`. ICE candidates trickle; queued until remote SDP exists.
8. **ontrack.** Hidden `<video>`/`<audio>` attached. C# `OnTrack` builds `VideoStreamTrack`. `WebRTC.Update` copies pixels into Unity textures. Browser plays remote audio.
9. **Leave.** `ContextDeleteMediaStreamTrack` / `uwcom_releaseMediaTrack` stops tracks and removes DOM nodes.

If join hangs, first check: pump running, `Task.Yield`, no Promise `dynCall`, websocket actually Open, Twirp Content-Type.

If video is black: `WebRTC.Update` running, canvas/video elements in DOM (DevTools), `GL.textures[ptr]` defined, dimensions non-zero (`onloadedmetadata`).

If audio is silent: click the page, `AudioContext.state === "running"`, remote `<audio>` `paused === false`.

---

## Part 8 — What this prototype still does not do

Do not assume these work because the branch “has WebGL support”:

- Real browser microphone via `getUserMedia` (dummy device only).
- `GetStats` (throws).
- Native audio bindings / `AudioCustomFilter` path.
- Frame transformers, hardware encoder selection, batch render events (stubs return 0).
- Robust JS-owned texture IDs (`CreateNativeTexture` scan is a trap).
- Async `readPixels` (code present, commented, unfinished).
- Device enumeration via `navigator.mediaDevices.enumerateDevices`.
- Production Safari/Firefox certification.
- Same Editor Play Mode path (still native webrtc).

`Context.jslib` is full of `// TODO` empty bodies. That is OK if C# never calls them on WebGL. Before calling a NativeMethod from a new feature, grep the jslib.

---

## Part 9 — If you reimplement from scratch, suggested milestones

Ship each milestone as a WebGL player you can click, not as a pile of jslib.

1. **Empty player + asmdef + `__Internal` + `ContextCreate`.** Confirm the build links.
2. **HTTP + WS connect to coordinator.** No WebRTC yet. Proves CORS, timeout, connect gate, location hint fallback.
3. **RTCPeerConnection + createOffer/setLocal/setRemote pumped from Update.** Log SDP in the browser console. No media.
4. **ICE candidates + SFU answer.** Datachannel or empty transceivers. `connectionState === "connected"`.
5. **Remote video only.** Subscribe, hidden `<video>`, `texImage2D` into a `RawImage`.
6. **Local video.** Canvas `captureStream` from a Unity camera RT.
7. **Remote audio + autoplay unlock.**
8. **Local mic getUserMedia + replaceTrack.**
9. **Device names, permissions, lobby preview.**
10. **Leave/dispose with no leftover DOM captures.**

Do not start at 5. Pixel work hides SDP/HTTP bugs.

---

## Part 10 — File map (this branch)

Unity.WebRTC (the integration):

```
Packages/StreamVideo/Runtime/Libs/io.stream.unity.webrtc/
  Runtime/Scripts/WebRTC.cs              Lib, Init callbacks, Update pump
  Runtime/Scripts/WebGLSessionOps.cs     SDP pump, JSON DTOs, PtrToIntPtrArray
  Runtime/Scripts/Context.cs             CreateOffer enqueue + JSON options
  Runtime/Scripts/RTCPeerConnection.cs   WebGL return shapes, ICE ctor, GetStats stub
  Runtime/Scripts/VideoStreamTrack.cs    RenderLocalVideotrack / UpdateRendererTexture
  Runtime/Scripts/*Observer.cs           CreateWithHandle for synthetic ptrs
  Runtime/Plugins/WebGL/*.jslib          browser ABI
  Runtime/Plugins/WebGL/adapter.jspre    webrtc-adapter
```

SDK (browser sandbox, not WebRTC):

```
Runtime/Libs/Http/UnityWebRequestHttpClient.cs
Runtime/Libs/Http/UnityWebRequestHttpMessageHandler.cs
Runtime/Libs/Websockets/NativeWebSocketWrapper.cs
Runtime/Libs/Websockets/WebsocketConnectGate.cs
Runtime/Core/LowLevelClient/StreamVideoLowLevelClient.cs   hint fallback, Twirp handler
Runtime/Core/LowLevelClient/UnityWebRtcWrapperExtensions.cs  Yield + pump
Runtime/Core/DeviceManagers/StreamAudioDeviceManager.cs
Runtime/Core/DeviceManagers/StreamVideoDeviceManager.cs
Samples~/VideoChat/Scripts/UI/UIManager.cs
Samples~/VideoChat/Scripts/UI/PermissionsManager.cs
Samples~/VideoChat/Scripts/UI/Devices/MediaDevicePanelBase.cs
Tests/Editor/WebGLSupportTests.cs
Tests/Editor/WebsocketConnectGateTests.cs
```

---

## Part 11 — Concepts checklist (you should be able to explain each)

After reading, you should be able to answer without opening files:

- Why one C# API with a jslib backend beats wrapping `stream-video-js`.
- Why `#if UNITY_WEBGL && !UNITY_EDITOR` and not `#if UNITY_WEBGL`.
- What `.jslib` vs `.jspre` do, and why `mergeInto` + `autoAddDeps` exist.
- How `IntPtr` maps to a JS object.
- Why Promise → `dynCall(string)` hangs, and how the pending-SDP pump avoids it.
- Why `Task.Delay` never resumes and `Task.Yield` does.
- Why remote video needs a hidden `<video>` and local video needs a hidden `<canvas>`.
- Why `GL.textures[GetNativeTexturePtr()]` is the pixel bridge.
- Why HTTP CORS looks like `Unknown Error`.
- Why `Connect()` is not connected.
- Why `Microphone` cannot appear in WebGL player code.
- Why a click must happen before getUserMedia and audio playback.

If any of those is fuzzy, re-read the matching Part 4–5 section, then open the file in the map. Then attempt milestone 1.
