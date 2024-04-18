using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Libs.Utils;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class CameraMediaDevicePanel : MediaDevicePanelBase<CameraDeviceInfo>
    {
        protected override IEnumerable<CameraDeviceInfo> GetDevices() => Client.VideoDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(CameraDeviceInfo device) => device.Name;

        protected override void OnInit()
        {
            base.OnInit();

            TrySelectFirstWorkingDeviceAsync().LogIfFailed();
        }

        private async Task TrySelectFirstWorkingDeviceAsync()
        {
            var workingDevice = await Client.VideoDeviceManager.TryFindFirstWorkingDeviceAsync();
            if (!workingDevice.HasValue)
            {
                Debug.LogError("No working camera found");
                return;
            }

            SelectDeviceWithoutNotify(workingDevice.Value);
            SelectedDevice = workingDevice.Value;
        }
    }
}