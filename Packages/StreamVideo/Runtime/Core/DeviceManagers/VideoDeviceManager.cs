using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Logs;
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
        //StreamTodo: user can add/remove devices, we might want to expose DeviceAdded, DeviceRemoved events
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
                Enable();
            }
        }

        //StreamTodo: better to not expose this and make fake tracks for local user. This way every participant is processed exactly the same
        /// <summary>
        /// Get the instance of <see cref="WebCamTexture"/> for the selected device. This is useful if you want to 
        ///
        /// This can change whenever a selected device is changed. Subscribe to <see cref="DeviceManagerBase{TDeviceInfo}.SelectedDeviceChanged"/> to get notified when the selected device changes.
        /// </summary>
        public WebCamTexture GetSelectedDeviceWebCamTexture() => _activeCamera;

        internal VideoDeviceManager(RtcSession rtcSession, IInternalStreamVideoClient client, ILogs logs)
            : base(rtcSession, client, logs)
        {
        }

        protected override void OnSetEnabled(bool isEnabled)
        {
            if (isEnabled && _activeCamera != null && !_activeCamera.isPlaying)
            {
                _activeCamera.Play();
                Client.SetCameraInputSource(_activeCamera);
            }

            if (!isEnabled && _activeCamera != null)
            {
                _activeCamera.Stop();
            }
            
            RtcSession.TrySetVideoTrackEnabled(isEnabled);
        }

        protected override async Task<bool> OnTestDeviceAsync(CameraDeviceInfo device, int msTimeout)
        {
            WebCamTexture camTexture = null;
            try
            {
                camTexture = new WebCamTexture(device.Name);
                
                // This can fail and the only result will be Unity logging "Could not start graph" and "Could not pause pControl" - these are logs and not exceptions.
                camTexture.Play();

                if (_stopwatch == null)
                {
                    _stopwatch = new Stopwatch();
                }

                _stopwatch.Stop();
                _stopwatch.Reset();
                _stopwatch.Start();

                var isCapturing = false;

                //StreamTodo: Investigate https://forum.unity.com/threads/get-webcamtexture-pixel-data-without-using-getpixels32.1315821/

                Color[] frame1 = null, frame2 = null;

                while (_stopwatch.ElapsedMilliseconds < msTimeout)
                {
                    //WebCamTexture.didUpdateThisFrame does not guarantee that camera is capturing data. We need to compare frames
                    if (camTexture.didUpdateThisFrame)
                    {
                        var frame = camTexture.GetPixels();
                        
                        if (frame1 == null)
                        {
                            if (!IsFrameBlack(frame))
                            {
                                frame1 = frame;
                                continue;
                            }
                        }
                        else
                        {
                            if (!IsFrameBlack(frame))
                            {
                                frame2 = frame;
                            }
                        }
                    }

                    if (frame1 != null && frame2 != null && !AreFramesEqual(frame1, frame2))
                    {
                        isCapturing = true;
                        break;
                    }

                    await Task.Delay(1);
                }
                
                return isCapturing;
            }
            catch (Exception e)
            {
                Logs.Error(e.Message);
                return false;
            }
            finally
            {
                if (camTexture != null)
                {
                    camTexture.Stop();
                    Object.Destroy(camTexture);
                }
            }
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

        private WebCamTexture _activeCamera;
        private Stopwatch _stopwatch;

        private bool IsNewInstanceNeeded(CameraDeviceInfo device, VideoResolution resolution, int fps = 30)
        {
            return _activeCamera == null || _activeCamera.requestedWidth != resolution.Width ||
                   _activeCamera.requestedHeight != resolution.Height ||
                   Mathf.Abs(_activeCamera.requestedFPS - fps) < 0.01f;
        }
        
        private static bool AreFramesEqual(IReadOnlyList<Color> frame1, IReadOnlyList<Color> frame2)
        {
            if (frame1.Count != frame2.Count)
            {
                return false;
            }

            for (var i = 0; i < frame1.Count; i++)
            {
                if (frame1[i] != frame2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFrameBlack(IReadOnlyList<Color> frame1)
        {
            for (var i = 0; i < frame1.Count; i++)
            {
                //StreamTodo: perhaps check if the whole frame is same color. In one case a virtual camera was solid orange
                if (frame1[i] != Color.black)
                {
                    return false;
                }
            }

            return true;
        }
    }
}