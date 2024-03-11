using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class CameraMediaDevicePanel : MediaDevicePanelBase
    {
        protected override IEnumerable<string> GetDevicesNames() => WebCamTexture.devices.Select(d => d.name);
    }
}