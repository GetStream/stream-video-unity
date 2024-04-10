using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Core.DeviceManagers
{
    // StreamTodo: write tests:
    /* - change in video res & FPS needs to be reflected in sent video
- If you disable track before call it should stay disabled during the call
- disabling camera should disable the video track (same with mic)
- enabling the camera should enable the video track again (same with mic)
- changing a disabled camera should not enable it
- test that monitoring for video devices works and deviceAdded, deviceRemoved events are fired accordingly
- test that enabling device triggers capturing and disabling stops capturing
*/
    internal class VideoDeviceManager : DeviceManagerBase<CameraDeviceInfo>, IVideoDeviceManager
    {
        public bool IsCapturing => _activeCamera != null && _activeCamera.isPlaying;

        public override IEnumerable<CameraDeviceInfo> EnumerateDevices()
        {
            foreach (var device in WebCamTexture.devices)
            {
                yield return new CameraDeviceInfo(device.name, device.isFrontFacing, this);
            }
        }

        public void SelectDevice(CameraDeviceInfo device, int fps = 30)
            => SelectDevice(device, VideoResolution.Res_720p, fps);

        public void SelectDevice(CameraDeviceInfo device, VideoResolution requestedResolution, int requestedFPS = 30)
        {
            if (!device.IsValid)
            {
                throw new ArgumentException($"{nameof(device)} argument is not valid. The device name is empty.");
            }
            
            var deviceChanged = SelectedDevice != device;
            var newInstanceNeeded = IsNewInstanceNeeded(device, requestedResolution);
            
            if (_activeCamera != null && _activeCamera.isPlaying)
            {
                _activeCamera.Stop();
            }

            if (newInstanceNeeded)
            {
                _activeCamera = new WebCamTexture(device.Name, (int)requestedResolution.Width, (int)requestedResolution.Height, requestedFPS);
                SelectedDevice = device;
                
                // we probably need to make this internal so we don't end up out of sync if they select a device + set cam input source
                Client.SetCameraInputSource(_activeCamera);
            }
            else
            {
                if (deviceChanged)
                {
                    _activeCamera.deviceName = device.Name;
                    SelectedDevice = device;
                }
            }

            if (IsEnabled)
            {
                _activeCamera.Play();
            }
        }

        /// <summary>
        /// Inject your own instance of <see cref="WebCamTexture"/> to be used as an active camera.
        /// Use this only if you need to control the instance of <see cref="WebCamTexture"/>. Otherwise, simply use the <see cref="SelectDevice"/>
        /// </summary>
        /// <param name="webCamTexture"></param>
        private void SetRawWebCamTexture(WebCamTexture webCamTexture)
        {
            //StreamTodo: implement and make public
        }
        
        internal VideoDeviceManager(RtcSession rtcSession, IInternalStreamVideoClient client)
            : base(rtcSession, client)
        {
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetVideoTrackEnabled(isEnabled);
        
        protected override async Task<bool> OnTestDeviceAsync(CameraDeviceInfo device, int msTimeout)
        {
            var camTexture = new WebCamTexture(device.Name);
            camTexture.Play();
            
            //StreamTodo: check in loop and exit early if device is working already
            await Task.Delay(msTimeout);

            // Simple check for valid texture size
            var isStreaming = camTexture.width > 16 && camTexture.height > 16; 
    
            camTexture.Stop();
            Object.Destroy(camTexture);
            return isStreaming;
        }

        protected override void OnDisposing()
        {
            if (_activeCamera != null)
            {
                if (_activeCamera.isPlaying)
                {
                    _activeCamera.Stop();
                }
                
                Object.Destroy(_activeCamera);
            }
            
            base.OnDisposing();
        }

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