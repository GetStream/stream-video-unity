using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using UnityEngine;

namespace StreamVideo.Core.DeviceManagers
{
    internal class VideoDeviceManager : DeviceManagerBase<CameraDeviceInfo>, IVideoDeviceManager
    {
        internal VideoDeviceManager(RtcSession rtcSession)
            : base(rtcSession)
        {
        }

        public override IEnumerable<CameraDeviceInfo> EnumerateDevices()
        {
            foreach (var device in WebCamTexture.devices)
            {
                yield return new CameraDeviceInfo(device.name, device.isFrontFacing);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override async Task<bool> IsDeviceStreamingAsync(CameraDeviceInfo device)
        {
            var camTexture = new WebCamTexture(device.Name);
            camTexture.Play();
            await Task.Delay(200);

            // Simple check for valid texture size
            var isStreaming = camTexture.width > 16 && camTexture.height > 16; 
    
            camTexture.Stop();
            Object.Destroy(camTexture);
            return isStreaming;
        }

        public void SelectDevice(CameraDeviceInfo device, int fps = 30)
            => SelectDevice(device, VideoResolution.Res_720p, fps);

        public void SelectDevice(CameraDeviceInfo device, VideoResolution resolution, int fps = 30)
        {
            var deviceChanged = _activeCamera == null || _activeCamera.name != device.Name;
            var newInstanceNeeded = IsNewInstanceNeeded(device, resolution);
            
            if (_activeCamera != null && _activeCamera.isPlaying)
            {
                _activeCamera.Stop();
            }

            if (newInstanceNeeded)
            {
                _activeCamera = new WebCamTexture(device.Name, (int)resolution.Width, (int)resolution.Height, fps);
            }
            else
            {
                if (deviceChanged)
                {
                    _activeCamera.deviceName = device.Name;
                }
            }

            if (IsEnabled)
            {
                _activeCamera.Play();
            }
            
            //StreamTodo: set as a track input source
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetVideoTrackEnabled(isEnabled);
        
        //StreamTodo: wrap all Unity webcam texture operations here. Enabling/Disabling tracks should manage the WebCamTexture so that users only 
        //Also take into account that user may want to provide his instance of WebCamTexture + monitor for devices list changes 
        
        //StreamTodo: add AutoDetectActiveDevice() method -> will sample each device and pick the first that delivers data
        //We could also favor front camera on mobile devices
        
        private WebCamTexture _activeCamera;
        
        private bool IsNewInstanceNeeded(CameraDeviceInfo device, VideoResolution resolution, int fps = 30)
        {
            return _activeCamera == null || _activeCamera.requestedWidth != resolution.Width ||
                   _activeCamera.requestedHeight != resolution.Height ||
                   Mathf.Abs(_activeCamera.requestedFPS - fps) < 0.01f;
        }
    }
}