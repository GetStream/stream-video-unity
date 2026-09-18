using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
#if STREAM_DEBUG_ENABLED
using StreamVideo.Core.BackgroundFilters;
#endif
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
    internal class StreamVideoDeviceManager : DeviceManagerBase<CameraDeviceInfo>, IStreamVideoDeviceManager
    {
        public override event DeviceEnabledChangeHandler IsEnabledChanged;

        public override bool IsEnabled
        {
            get => RtcSession.PublisherVideoTrackIsEnabled;
            protected set => RtcSession.TrySetPublisherVideoTrackEnabled(value);
        }
        
        //StreamTodo: user can add/remove devices, we might want to expose DeviceAdded, DeviceRemoved events
        public override IEnumerable<CameraDeviceInfo> EnumerateDevices()
        {
            foreach (var device in WebCamTexture.devices)
            {
                yield return new CameraDeviceInfo(device.name, device.isFrontFacing, this);
            }
        }

        public void SelectDevice(CameraDeviceInfo device, bool enable, int requestedFPS = 30)
            => SelectDevice(device, VideoResolution.Res_720p, enable, requestedFPS);

        public void SelectDevice(CameraDeviceInfo device, VideoResolution requestedResolution, bool enable, int requestedFPS = 30)
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
            
            if (IsEnabled && enable && _activeCamera != null && !_activeCamera.isPlaying)
            {
                //OnSetEnabled will not trigger because IsEnabled value didn't change
                _activeCamera.Play();
                Client.SetCameraInputSource(_activeCamera);
            }

#if STREAM_DEBUG_ENABLED
            CameraOrientationDebug.Log(Logs, "camera.select",
                "device=" + device.Name + " front=" + device.IsFrontFacing + " enable=" + enable
                + " requested=" + requestedResolution.Width + "x" + requestedResolution.Height + "@" + requestedFPS
                + " | " + CameraOrientationDebug.DescribeWebCam(_activeCamera)
                + " | " + CameraOrientationDebug.DescribeScreen());
#endif

            SetEnabled(enable);
        }

        //StreamTodo: better to not expose this and make fake tracks for local user. This way every participant is processed exactly the same
        /// <summary>
        /// Get the instance of <see cref="WebCamTexture"/> for the selected device. This is useful if you want to 
        ///
        /// This can change whenever a selected device is changed. Subscribe to <see cref="DeviceManagerBase{TDeviceInfo}.SelectedDeviceChanged"/> to get notified when the selected device changes.
        /// </summary>
        public WebCamTexture GetSelectedDeviceWebCamTexture() => _activeCamera;

        internal StreamVideoDeviceManager(RtcSession rtcSession, IInternalStreamVideoClient client, ILogs logs)
            : base(rtcSession, client, logs)
        {
            RtcSession.PublisherVideoTrackIsEnabledChanged += OnPublisherVideoTrackIsEnabledChanged;
            RtcSession.PublisherVideoTrackChanged += OnPublisherVideoTrackChanged;
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
            RtcSession.PublisherVideoTrackIsEnabledChanged -= OnPublisherVideoTrackIsEnabledChanged;
            RtcSession.PublisherVideoTrackChanged -= OnPublisherVideoTrackChanged;

            
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
        private bool _restartingCamera;
        private bool _recreateOnNextEnable;

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
        
        /// <summary>
        /// Recreate only after a playing capture was Stop()'d (background). First enable must Play()
        /// the existing WebCamTexture; recreating it yields 16x16 until the session starts.
        /// </summary>
        internal static bool ShouldRecreateWebCamTexture(bool isMobilePlayer, bool isPlaying, bool captureWasStopped)
            => isMobilePlayer && !isPlaying && captureWasStopped;

        private void UpdateVideoHandling()
        {
            if (_restartingCamera || _activeCamera == null)
            {
                return;
            }

            var isEnabled = RtcSession.PublisherVideoTrackIsEnabled;
            if (!isEnabled)
            {
                if (_activeCamera.isPlaying)
                {
                    _activeCamera.Stop();
                }

                // Sample Pause may Stop() before SetEnabled(false). Recreate on the next
                // enable regardless of isPlaying — iOS Play() after Stop() does not restore frames.
                _recreateOnNextEnable = IsMobilePlayer;
                return;
            }

            if (_activeCamera.isPlaying)
            {
                return;
            }

            // iOS/Android capture sessions do not survive backgrounding. Play() on the
            // same WebCamTexture reports isPlaying but never delivers frames.
            if (ShouldRecreateWebCamTexture(IsMobilePlayer, _activeCamera.isPlaying, _recreateOnNextEnable))
            {
                _recreateOnNextEnable = false;
                RestartActiveCamera();
                return;
            }

            _activeCamera.Play();
            Client.SetCameraInputSource(_activeCamera);
        }

        private void RestartActiveCamera()
        {
            if (_restartingCamera)
            {
                return;
            }

            _restartingCamera = true;
            try
            {
                var deviceName = _activeCamera.deviceName;
                var width = _activeCamera.requestedWidth;
                var height = _activeCamera.requestedHeight;
                var fps = (int)_activeCamera.requestedFPS;
                if (fps <= 0)
                {
                    fps = 30;
                }

                if (_activeCamera.isPlaying)
                {
                    _activeCamera.Stop();
                }

                Object.Destroy(_activeCamera);
                _activeCamera = new WebCamTexture(deviceName, width, height, fps);
                _activeCamera.Play();
                Client.SetCameraInputSource(_activeCamera);
                RaiseSelectedDeviceChanged(SelectedDevice, SelectedDevice);
            }
            finally
            {
                _restartingCamera = false;
            }
        }

        private static bool IsMobilePlayer
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
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
        
        private void OnPublisherVideoTrackIsEnabledChanged(bool isEnabled)
        {
            UpdateVideoHandling();
            IsEnabledChanged?.Invoke(isEnabled);
        }
        
        private void OnPublisherVideoTrackChanged() => UpdateVideoHandling();
    }
}