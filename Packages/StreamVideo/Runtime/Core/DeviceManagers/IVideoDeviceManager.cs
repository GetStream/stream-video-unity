using UnityEngine;

namespace StreamVideo.Core.DeviceManagers
{
    //StreamTodo: revise setting 3D Scene Camera as a video source
    /// <summary>
    /// Manages interactions with video recording devices - Cameras.
    /// </summary>
    public interface IVideoDeviceManager : IDeviceManager<CameraDeviceInfo>
    {
        //StreamTodo: probably move all members from IDeviceManager here so we can have all comments specifically about video or audio
        
        /// <summary>
        /// Select a camera device for video capturing.
        /// </summary>
        /// <param name="device">Camera device</param>
        /// <param name="enable">Enable this device (Start Capturing Video)</param>
        /// <param name="requestedFPS">Requested frame rate for the captured video. If the requested FPS is not supported by the camera, the closets available value will be selected</param>
        void SelectDevice(CameraDeviceInfo device, bool enable, int requestedFPS = 30);

        /// <summary>
        /// Select a camera device for video capturing.
        /// </summary>
        /// <param name="device">Camera device</param>
        /// <param name="requestedResolution">Requested video resolution for the captured video. If the requested resolution is not supported by the camera, the closest available one will be selected.</param>
        /// <param name="enable">Enable this device (Start Capturing Video)</param>
        /// <param name="requestedFPS">Requested frame rate for the captured video. If the requested FPS is not supported by the camera, the closets available value will be selected</param>
        void SelectDevice(CameraDeviceInfo device, VideoResolution requestedResolution, bool enable, int requestedFPS = 30);

        /// <summary>
        /// Get the instance of <see cref="WebCamTexture"/> for the selected device. This is useful if you want to 
        ///
        /// This can change whenever a selected device is changed. Subscribe to <see cref="DeviceManagerBase{TDeviceInfo}.SelectedDeviceChanged"/> to get notified when the selected device changes.
        /// </summary>
        WebCamTexture GetSelectedDeviceWebCamTexture();
    }
}