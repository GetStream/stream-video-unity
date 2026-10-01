using System.Linq;
using System.Threading.Tasks;
using StreamVideo.Core;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.ExampleProject.UIToolkit
{
    /// <summary>
    /// Requests camera and microphone permissions and selects the initial devices.
    /// Devices are selected without changing their enabled state, which is controlled in the lobby.
    /// </summary>
    internal sealed class DeviceSelector
    {
        public DeviceSelector(VideoChatApp app, IStreamVideoClient client, int senderVideoFps)
        {
            _app = app;
            _client = client;
            _senderVideoFps = senderVideoFps;
            _permissions = new PermissionsManager(app);
        }

        public void RequestPermissionsAndSelectDevices()
        {
            if (_permissions.HasPermission(PermissionsManager.PermissionType.Camera))
            {
                SelectCameraAsync().LogIfFailed();
            }
            else
            {
                _permissions.RequestPermission(PermissionsManager.PermissionType.Camera,
                    onGranted: () => SelectCameraAsync().LogIfFailed(),
                    onDenied: () => Debug.LogError("Camera permission was not granted. Video capturing will not work."));
            }

            if (_permissions.HasPermission(PermissionsManager.PermissionType.Microphone))
            {
                SelectMicrophone();
            }
            else
            {
                _permissions.RequestPermission(PermissionsManager.PermissionType.Microphone,
                    onGranted: SelectMicrophone,
                    onDenied: () => Debug.LogError("Microphone permission was not granted. Audio capturing will not work."));
            }
        }

        private readonly VideoChatApp _app;
        private readonly IStreamVideoClient _client;
        private readonly int _senderVideoFps;
        private readonly PermissionsManager _permissions;

        private async Task SelectCameraAsync()
        {
            var videoDeviceManager = _client.VideoDeviceManager;
            if (!videoDeviceManager.EnumerateDevices().Any())
            {
                Debug.LogError("No camera devices found! Video streaming will not work.");
                return;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            foreach (var device in videoDeviceManager.EnumerateDevices())
            {
                if (!device.IsFrontFacing)
                {
                    continue;
                }

                if (await videoDeviceManager.TestDeviceAsync(device))
                {
                    videoDeviceManager.SelectDevice(device, _app.SenderVideoResolution, videoDeviceManager.IsEnabled,
                        _senderVideoFps);
                    return;
                }
            }
#endif

            var workingDevice = await videoDeviceManager.TryFindFirstWorkingDeviceAsync();
            var selected = workingDevice ?? videoDeviceManager.EnumerateDevices().First();
            if (!workingDevice.HasValue)
            {
                Debug.LogWarning("No working camera found. Falling back to the first device: " + selected);
            }

            videoDeviceManager.SelectDevice(selected, _app.SenderVideoResolution, videoDeviceManager.IsEnabled,
                _senderVideoFps);
        }

        private void SelectMicrophone()
        {
            var audioDeviceManager = _client.AudioDeviceManager;
            var microphone = audioDeviceManager.EnumerateDevices().FirstOrDefault();
            if (microphone == default)
            {
                Debug.LogError("No microphone devices found! Audio streaming will not work.");
                return;
            }

            audioDeviceManager.SelectDevice(microphone, audioDeviceManager.IsEnabled);
        }
    }
}
