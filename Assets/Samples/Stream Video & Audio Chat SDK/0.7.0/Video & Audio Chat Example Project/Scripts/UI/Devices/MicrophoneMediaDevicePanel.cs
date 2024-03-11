using System.Collections.Generic;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MicrophoneMediaDevicePanel : MediaDevicePanelBase
    {
        protected override IEnumerable<string> GetDevicesNames() => Microphone.devices;
    }
}