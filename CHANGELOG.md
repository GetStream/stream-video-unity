v0.7.0:

0.7.0:

## Improvements:
- Added option to toggle local user camera/microphone track on/off
Improved documentation
- Added option to control video resolution per call participant - essential for bandwidth optimization. Called via 
- Added option to control published video resolution and FPS - the resolution & FPS are copied from the passed WebCamTexture instance
- Improved setting of custom data for call and participant objects
- Added sorted participants property to the call object -> [Call state Docs](https://getstream.io/video/docs/unity/guides/call-and-participant-state/#properties)
- Improved state management
- Added option to pin participants in a call (this state is reflected in sorted

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
