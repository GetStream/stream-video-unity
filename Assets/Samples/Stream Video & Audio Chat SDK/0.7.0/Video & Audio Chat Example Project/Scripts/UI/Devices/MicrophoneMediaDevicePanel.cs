using System.Collections.Generic;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MicrophoneMediaDevicePanel : MediaDevicePanelBase<MicrophoneDeviceInfo>
    {
        protected override MicrophoneDeviceInfo SelectedDevice => Client.AudioDeviceManager.SelectedDevice;
        
        protected override bool IsDeviceEnabled
        {
            get => Client.AudioDeviceManager.IsEnabled;
            set
            {
                Client.AudioDeviceManager.SetEnabled(value);
                
                // Force set IOS audio settings StreamTODO: remove this 
                AudioSessionMonitor.Instance.Prepare();
            }
        }

        protected override IEnumerable<MicrophoneDeviceInfo> GetDevices() => Client.AudioDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(MicrophoneDeviceInfo device) => device.Name;
        
        protected override void ChangeDevice(MicrophoneDeviceInfo device)
        {
            Client.AudioDeviceManager.SelectDevice(device, IsDeviceEnabled);
            
            // Force set IOS audio settings StreamTODO: remove this 
            AudioSessionMonitor.Instance.Prepare();
        }

        protected override void OnInit()
        {
            base.OnInit();
            
            Client.AudioDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;
        }

        protected override void OnDestroying()
        {
            Client.AudioDeviceManager.SelectedDeviceChanged -= OnSelectedDeviceChanged;
            
            base.OnDestroying();
        }

        private void OnSelectedDeviceChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice) 
            => SelectDeviceWithoutNotify(currentDevice);
    }
}