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
