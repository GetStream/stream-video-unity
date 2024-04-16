using System.Collections.Generic;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MicrophoneMediaDevicePanel : MediaDevicePanelBase<MicrophoneDeviceInfo>
    {
        protected override IEnumerable<MicrophoneDeviceInfo> GetDevices() => Client.AudioDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(MicrophoneDeviceInfo device) => device.Name;
    }
}