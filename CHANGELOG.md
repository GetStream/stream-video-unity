0.11.0:

### Improvements

- Remote video subscriptions are now more reliable when participants join a call at different times — the SDK waits until a participant is actually publishing video before requesting their stream, and automatically re-subscribes when they start publishing
- Improved handling of large calls where remote media can arrive before full participant information is available — tracks are held and matched once the participant is known
- Subscriber connection setup is now serialized, preventing overlapping negotiations from interfering with each other

### Bug Fixes

- Fixed remote video not appearing when a participant starts publishing their camera after others have already joined the call
- Fixed subscription changes being lost when made while a previous subscription request was still in progress
- Fixed remote video tracks being dropped when they arrived before the participant's track identifier was available
- Fixed ICE restart not being applied to the connection that receives remote media, which could leave video stuck after a network recovery
- Fixed incorrect parsing of remote track identifiers in edge cases that could prevent video from binding to the right participant
- Fixed occasional crash when collecting WebRTC call statistics
- Fixed Unity console warnings when reading camera rotation before the camera has finished initializing

0.10.0:

### iOS Audio

- Added active echo cancellation and noise reduction for the iOS platform
- The SDK automatically configures the iOS audio session for voice calls — prioritizing low latency and best voice quality
- When leaving a call, the audio session is restored so other apps can resume audio normally
- **Recommended:** set Unity **Project Settings → Audio → DSP Buffer Size** to **Best Latency** (or **Good Latency**) before starting a call. If Unity locks a large audio buffer first, its session configuration can conflict with the SDK and reduce echo-cancellation effectiveness

### Improvements

- Added `client.CallLeaving` event — fired while the call is still active, before state is cleared. Use this when you need access to call data during the leave flow; `CallEnded` fires after the call state has already been reset
- Added support for fetching demo credentials (API key + user token) for quick testing and sample apps — not intended for production use
- Published video quality now adapts dynamically when the server requests a quality change during a call
- Remote participant video can now be paused and resumed by the server when needed (e.g. to save bandwidth)
- Expanded debug tracing and fixed WebRTC stats reporting for more accurate call diagnostics in the Stream dashboard

### Bug Fixes

- Fixed reconnection stopping permanently after a brief network drop during an in-progress reconnect
- Fixed occasional crash when disposing the client while a call update was still in progress
- Fixed being unable to join again after a previous join attempt failed

0.9.0:

### Reconnection Flow

- The SDK now automatically handles reconnection when the connection to the video server is lost during an active call. The reconnection uses a multi-strategy approach:
  - **Fast Reconnect** — attempts to quickly re-establish the connection while preserving the existing WebRTC connections. This minimizes disruption and allows the call to resume almost instantly with audio and video tracks automatically restored.
  - **Full Rejoin** — if Fast Reconnect fails (after multiple attempts or a timeout deadline), the SDK falls back to a full rejoin — creating new WebRTC connections and re-publishing all tracks automatically.
  - The SDK starts with Fast Reconnect and automatically escalates to Full Rejoin when needed. Previously published tracks and subscriptions are restored automatically after a successful reconnect.
- When the device goes back online (e.g., after toggling airplane mode or losing Wi-Fi), the SDK automatically initiates reconnection if a call was active.

### Improvements

- Call capabilities are now updated in real-time when changed server-side
- Participant connection quality is now tracked and updated from the server
- Participant state changes are now properly reflected when updated server-side
- Trigger connection restart if requested by the server
- The call is now properly closed when the server signals that the call has ended
- WebSocket `Disconnected` event is now always fired from the Unity main thread, preventing potential threading issues in event handlers

### Bug Fixes

- Fixed `RenderTexture` leak — creating `RenderTexture` multiple times during a single session was not releasing the old ones
- Fixed `LeaveCallAsync` getting stuck on the stats collection task
- Fixed a race condition when attempting to connect while the underlying WebSocket is already connecting
- Fixed editor errors when destroying audio containers in edit mode

0.8.22:

- Added `participant.IsSpeakingChanged` and `participant.AudioLevelChanged` events to notify when the participant starts/stops speaking or when the volume changes

0.8.21:

- Added an option to mute an audio track locally. This mute is applied only to the current audio track on the local device. Note that a new audio track instance is created each time the same user leaves and rejoins the call. Therefore, it’s up to the integrator to cache the mute state and reapply it whenever the participant joins and adds an audio track. An example of caching can be found in this PR: https://github.com/GetStream/stream-video-unity/pull/203

0.8.20:

- Introduce `Client.SetAndroidAudioUsageMode` to allow setting Android audio mode (media or voice communication)
- Add `NativeAudioDeviceManager.GetAudioRoute()` to easily inspect current audio route on Android for debug purposes

0.8.19:

- Fix for this crash in `unity::webrtc::VideoFrameAdapter::ToI420`
- Potential fix for audio delay after pausing the app
- Fix SFU WebSocket reconnecting when Coordinator Websocket connection was lost

0.8.18:

- Force hardware AEC in calls
- Potential fix for audio delay after previously pausing/resuming the audio

0.8.17:

- Recompiled the native Android library with NDK 28 to fix missing 16KB alignment requirement for Android builds

0.8.16:

- from now on, each participant needs to explicitly set which tracks of other participants he wants to request by calling: `participant.SetIncomingVideoEnabled`. This needs to be set for all `call.Participants` when joining the call and also in reaction to `call.ParticipantJoined` event. Previously, the SDK was auto-subscribing to every joined participants but there's a server limit of 40 subscriptions. Audio subscriptions have no limit, but can also be controlled.
`participant.SetIncomingVideoEnabled` - request receiving video for this participant
`participant.SetIncomingAudioEnabled` - request receiving audio for this participant
- A typical pattern is to control the video request based on the rendering state of the UI. So the video should only be requested for participants who are currently rendered on screen. The rendering resolution should be passed to `participant.UpdateRequestedVideoResolution` to request video resolution matching the rendered resolution.

0.8.15:

- Add support for cancellation tokens. GetCallAsync/JoinCallAsync/ConnectUserAsync operations can now be cancelled via CancellationToken
- call.LeaveAsync will cancel any previous in-progress join operation
- Fixed WS reconnection issue
- Improved WebSocket disconnection handling
- Added an additional callback when the video server had disconnected.

0.8.14:

- Change `call.GetLocalParticipant()` to return null if local participant is not found
- potential fix for missing local participant in `call.Participants`
- Added `client.PauseAndroidAudioPlayback()` and `client.ResumeAndroidAudioPlayback()` methods to stop/resume all audio played by the SDK on Android. This is for better handling when the app is minimized.

0.8.13:

- Fix `call.Participants` sometimes not containing the local participant.

0.8.12:

- Better handling of `LeaveCallAsync`. This should solve the "`LeaveCallAsync` takes a long time sometimes"
- Respect video resolution set via SelectDevice. Previously, the broadcast video resolution was fixed.
- Potential crash fix in the native plugin
- Improvement to the broadcasted video handling - this can potentially improve video performance for watching users

0.8.11:

- Fix `NullReferenceException` in `SubscribeToTracksAsync` 
- Revert kicking the user out of the call when the SFU WebSocket disconnects

0.8.10:

- Fix `track.EnabledChanged` not firing for the first change

0.8.9:

- Fix `_videoTrack.EnabledChange` callback
- Add `the IStreamCall.ParticipantCount` - presents participants count to any call size (`IStreamCall.Participants` shows first 250 participants)
- Fix participant leaving the call not being immediately signaled to other users

0.8.8:

- Add stats and debug info monitoring - this will be available to browse in our new dashboard for debugging purposes
- Fix `InvalidOperationException: Collection was modified`  when the call object is updated
- Fix the black screen appearing for a watcher user when the broadcaster disables the video track before leaving the call and enables it when entering the call

0.8.7:

- Upgraded internal com.unity.webrtc package to the latest version
- The most important change here is "Support 16kb pagesizes for Android"

0.8.6:

## Improvements:
- Optimized video track
- Fixed clearing participants tracks allocations
- Improve participant leaving the call detection -> other users will be immediately notified if the leaving user left gracefuly
- Fix Android crash in the C++ layer when the AudioTrackSinkAdapter destructor is called

0.8.5:

- Fix `ArgumentNullException: Value cannot be null. Parameter name: value in UserId` exception when building `TrackSubscriptionDetails`

0.8.4:

## Android improvements:
- Fix user muting -> previously, a muted audio track would break permanently
- Add `call.MuteSelf(audio: true, video: false, screenShare: false))` and `call.MuteOthers(audio: true, video: false, screenShare: false)` helpers methods for easier muting of self or others' audio/video tracks
- Optimize + reduce build size
- Enabled built-in, hardware echo cancellation capabilities. This affects modern devices with the built-in AEC module.

0.8.3
## Android improvements:
- Enabled built-in, hardware echo cancellation capabilities. This affects modern devices with the built-in AEC module.

0.8.2
## Added:
- Enabled generating developer tokens. This allows generating authentication tokens inside the Unity app without needing a backend server. This is only suitable for the development phase and should not be used in production. The newly available static methods are: `StreamVideoClient.CreateDeveloperAuthToken(userId)` and `StreamVideoClient.SanitizeUserId(userName)` (for removing disallowed chars from a userId). This feature requires having the `Disable Auth Checks` flag enabled in Stream Dashboard.

0.8.1
- Fix IOS build process failing due to missing symbols like: `_StartAudioCapture`, `_StopAudioPlayback`, `_GetAudioProcessingModuleConfig`, `_PeerConnectionCanTrickleIceCandidates`

0.8.0

Android platform improvements
- Added echo cancellation and noise reduction to audio
- Fixed video rotation in portrait mode
- Fixed Unity ExecutionEngineException: Attempting to call method `StreamVideo.Libs.Serialization.NewtonsoftJsonSerializer::TryConvertTo<System.Single>` for which no ahead of time (AOT) code was generated.
- Improvements to call stability

0.7.0:

## Improvements:
- Added option to toggle local user camera/microphone track on/off
Improved documentation
- Added option to control video resolution per call participant - essential for bandwidth optimization. Called via `participant.UpdateRequestedVideoResolution`
- Added option to control published video resolution and FPS - the resolution & FPS are copied from the passed WebCamTexture instance
- Improved setting of custom data for call and participant objects
- Added sorted participants property to the call object -> [Call state Docs](https://getstream.io/video/docs/unity/guides/call-and-participant-state/#properties)
- Improved state management
- Added option to "pin" participants in a call (this state is reflected in "sorted participants")

## Bugfixes:
- Fixed Null Reference Exception when local camera is set to NULL
- Fixed Null Reference Exception when the client is disposed multiple times
- Fixed Null Reference Exception when the client is disposed during an async operation

## Example Project:
- Separated layout into two screens: pre-call and in-call screens
- Refactored call layout to present the dominant speaker in a big window and the rest of the participants in a scrollable list below
- Added camera/microphone controls to the Call Screen -> You can now toggle cam/mic on/off or change to another device during the call.
- Added devices monitoring - devices list will dynamically update if a device is removed or a new one is plugged in
- Fixed various UI bugs

v0.5.0:

Fixes:
* Fix null ref exception when previous dominant speaker is null

Improvements:
* Add support for changing the camera device during the call
* Add support for changing the microphone device during the call
* Update DTOs to the latest OpenAPI spec

Sample Project
* Fixes:
* Improvements:
	* Implement showing dominant speaker
	* Update getting the demo credentials to the latest requirements

v0.0.1:

Initial release
