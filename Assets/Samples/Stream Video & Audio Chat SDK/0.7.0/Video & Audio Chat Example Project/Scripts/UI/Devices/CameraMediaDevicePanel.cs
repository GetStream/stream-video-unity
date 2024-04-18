using System.Collections.Generic;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class CameraMediaDevicePanel : MediaDevicePanelBase<CameraDeviceInfo>
    {
        protected override CameraDeviceInfo SelectedDevice => Client.VideoDeviceManager.SelectedDevice;
        
        protected override bool IsDeviceEnabled
        {
            get => Client.VideoDeviceManager.IsEnabled;
            set => Client.VideoDeviceManager.SetEnabled(value);
        }

        protected override IEnumerable<CameraDeviceInfo> GetDevices() => Client.VideoDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(CameraDeviceInfo device) => device.Name;

        protected override void ChangeDevice(CameraDeviceInfo device) => Client.VideoDeviceManager.SelectDevice(device);

        protected override void OnInit()
        {
            base.OnInit();
            
            Client.VideoDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;
        }

        private void OnSelectedDeviceChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice) 
            => SelectDeviceWithoutNotify(currentDevice);
    }
}