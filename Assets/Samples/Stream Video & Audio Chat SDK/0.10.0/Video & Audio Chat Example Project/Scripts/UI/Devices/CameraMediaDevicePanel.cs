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

        protected override void ChangeDevice(CameraDeviceInfo device)
            => Client.VideoDeviceManager.SelectDevice(device, UIManager.SenderVideoResolution, IsDeviceEnabled,
                UIManager.SenderVideoFps);

        protected override void OnInit()
        {
            base.OnInit();

            Client.VideoDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;
            Client.VideoDeviceManager.IsEnabledChanged += OnIsEnabledChanged;
        }

        protected override void OnParentShow()
        {
            base.OnParentShow();

            if (SelectedDevice != default)
            {
                SelectDeviceWithoutNotify(SelectedDevice);
            }
        }

        protected override void OnParentHide()
        {
            base.OnParentHide();
        }
        
        protected override void OnDestroying()
        {
            Client.VideoDeviceManager.SelectedDeviceChanged -= OnSelectedDeviceChanged;
            
            base.OnDestroying();
        }

        private void OnSelectedDeviceChanged(CameraDeviceInfo previousDevice, CameraDeviceInfo currentDevice)
            => SelectDeviceWithoutNotify(currentDevice);
        
        private void OnIsEnabledChanged(bool isEnabled) => UpdateDeviceState(isEnabled);
    }
}