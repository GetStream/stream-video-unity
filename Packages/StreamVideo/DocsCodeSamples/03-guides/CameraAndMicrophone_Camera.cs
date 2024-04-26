using System.Linq;
using StreamVideo.Core;
using StreamVideo.Core.DeviceManagers;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

namespace StreamVideoDocsCodeSamples._03_guides
{
    internal class CameraAndMicrophone_Camera : MonoBehaviour
    {
        public void ListAvailableCameraDevices()
        {
var cameras = _client.VideoDeviceManager.EnumerateDevices();

foreach (var camera in cameras)
{
    Debug.Log(camera.Name); // Get camera name
}
        }

        public void SelectCamera()
        {
            // Get available camera devices. Returns IEnumerable<CameraDeviceInfo>
            var cameras = _client.VideoDeviceManager.EnumerateDevices();

            foreach (var cam in cameras)
            {
                Debug.Log(cam.Name); // Get the name of the camera
            }

            var camera = cameras.First();

_client.VideoDeviceManager.SelectDevice(camera, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, enable: true, requestedFPS: 24);
            
            
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_720p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_720p, enable: true, requestedFPS: 30);
        }

        public void VideoResolutionValues()
        {
            // Get available camera devices. Returns IEnumerable<CameraDeviceInfo>
            var cameras = _client.VideoDeviceManager.EnumerateDevices();

            
            var camera = cameras.First();
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_144p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_240p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_360p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_480p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_720p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, VideoResolution.Res_1080p, enable: true);
_client.VideoDeviceManager.SelectDevice(camera, new VideoResolution(500, 500), enable: true);
        }

        public void GetSelectedCamera()
        {
            var selectedCamera = _client.VideoDeviceManager.SelectedDevice;
        }

        public void StartStopCamera()
        {
            // Enable device to start capturing camera input
            _client.VideoDeviceManager.Enable();

            // Disable device to stop capturing camera input
            _client.VideoDeviceManager.Disable();

            // Set the enabled state by passing a boolean argument
            _client.VideoDeviceManager.SetEnabled(true);
        }

        public void GetLocalParticipantVideoPreview()
        {
var webCamTexture = _client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();

// You can attach this texture to RawImage UI Component
GetComponent<RawImage>().texture = webCamTexture;
        }

public void GetLocalParticipantVideoPreviewFull()
{
    // Triggered when the selected devices changes
    _client.VideoDeviceManager.SelectedDeviceChanged += UpdateLocalParticipantPreview;
}

private void UpdateLocalParticipantPreview(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
{
    var webCamTexture = _client.VideoDeviceManager.GetSelectedDeviceWebCamTexture();

    // You can attach this texture to RawImage UI Component
    GetComponent<RawImage>().texture = webCamTexture;
}

        public void CheckCameraStatus()
        {
            // Check if currently selected device is enabled
            var isDeviceEnabled = _client.VideoDeviceManager.IsEnabled;
        }

        public void VideoDeviceManagerEvents()
        {
            // Triggered when the selected devices changes
            _client.VideoDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;

            // Triggered when the IsEnabled property changes
            _client.VideoDeviceManager.IsEnabledChanged += OnIsEnabledChanged;
        }

        private void OnIsEnabledChanged(bool isEnabled)
        {
        }

        private void OnSelectedDeviceChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
        {
        }

        public void CameraTesting()
        {
            var cameras = _client.VideoDeviceManager.EnumerateDevices();
            var camera = cameras.First();

            // Testing devices

            _client.VideoDeviceManager.TestDeviceAsync(camera);

            _client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
        }

        public void CameraIOSPermissions()
        {
            // Request permission to use the Camera
            Application.RequestUserAuthorization(UserAuthorization.WebCam);

            // Check if user granted camera permission
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                // Notify user that camera permission was not granted and the camera capturing will not work.
            }
        }

        public void CameraAndroidPermissions()
        {
            // Request camera permissions
            Permission.RequestUserPermission(Permission.Camera);

            // Check if user granted camera permission
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // Notify user that camera permission was not granted and the camera capturing will not work.
            }
        }

        private IStreamVideoClient _client;
    }
}