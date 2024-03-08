using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal class VideoDeviceManager : DeviceManagerBase, IVideoDeviceManager
    {
        internal VideoDeviceManager(RtcSession rtcSession)
            : base(rtcSession)
        {
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetVideoTrackEnabled(isEnabled);
        
        //StreamTodo: wrap all Unity webcam texture operations here. Enabling/Disabling tracks should manage the WebCamTexture so that users only 
        //Also take into account that user may want to provide his instance of WebCamTexture + monitor for devices list changes 
        
        //StreamTodo: add AutoDetectActiveDevice() method -> will sample each device and pick the first that delivers data
        //We could also favor front camera on mobile devices
    }
}