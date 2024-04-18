using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.DeviceManagers;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MicrophoneMediaDevicePanel : MediaDevicePanelBase<MicrophoneDeviceInfo>
    {
        protected override IEnumerable<MicrophoneDeviceInfo> GetDevices() => Client.AudioDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(MicrophoneDeviceInfo device) => device.Name;

        protected override void OnInit()
        {
            base.OnInit();

            // Select first microphone by default
            var microphoneDevice = Client.AudioDeviceManager.EnumerateDevices().FirstOrDefault();
            if (microphoneDevice == default)
            {
                Debug.LogError("No microphone found");
                return;
            }
            
            SelectDeviceWithoutNotify(microphoneDevice);
            SelectedDevice = microphoneDevice;
            
        }
    }
}