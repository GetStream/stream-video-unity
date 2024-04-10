namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Manages interactions with video recording devices - Cameras.
    /// </summary>
    public interface IVideoDeviceManager : IDeviceManager<CameraDeviceInfo>
    {
        //StreamTodo: probably move all members from IDeviceManager here so we can have all comments specifically about video or audio
        
        void SelectDevice(CameraDeviceInfo device, int fps = 30);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="requestedResolution">Requested video resolution for the captured video. If the requested resolution is not supported by the camera, the closest available one will be selected.</param>
        /// <param name="requestedFPS">Requested frame rate for the captured video. If the requested FPS is not supported by the camera, the closets available one will be selected</param>
        void SelectDevice(CameraDeviceInfo device, VideoResolution requestedResolution, int requestedFPS = 30);
    }
}