namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Manages interactions with video recording devices - Cameras.
    /// </summary>
    public interface IVideoDeviceManager : IDeviceManager<CameraDeviceInfo>
    {
        //StreamTodo: probably move all members from IDeviceManager here so we can have all comments specifically about video or audio
        
        void SelectDevice(CameraDeviceInfo device, int fps = 30);

        void SelectDevice(CameraDeviceInfo device, VideoResolution resolution, int fps = 30);
    }
}