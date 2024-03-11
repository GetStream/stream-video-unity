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
