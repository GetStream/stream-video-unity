using System.Collections.Generic;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class CameraMediaDevicePanel : MediaDevicePanelBase<CameraDeviceInfo>
    {
        protected override IEnumerable<CameraDeviceInfo> GetDevices() => Client.VideoDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(CameraDeviceInfo device) => device.Name;
    }
}