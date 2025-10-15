﻿using System.Collections.Generic;
using StreamVideo.Core.DeviceManagers;

namespace StreamVideo.ExampleProject.UI.Devices
{
    public class MicrophoneMediaDevicePanel : MediaDevicePanelBase<MicrophoneDeviceInfo>
    {
        protected override MicrophoneDeviceInfo SelectedDevice => Client.AudioDeviceManager.SelectedDevice;
        
        protected override bool IsDeviceEnabled
        {
            get => Client.AudioDeviceManager.IsEnabled;
            set => Client.AudioDeviceManager.SetEnabled(value);
        }

        protected override IEnumerable<MicrophoneDeviceInfo> GetDevices() => Client.AudioDeviceManager.EnumerateDevices();

        protected override string GetDeviceName(MicrophoneDeviceInfo device) => device.Name;
        
        protected override void ChangeDevice(MicrophoneDeviceInfo device) => Client.AudioDeviceManager.SelectDevice(device, IsDeviceEnabled);
        
        protected override void OnInit()
        {
            base.OnInit();
            
            Client.AudioDeviceManager.SelectedDeviceChanged += OnSelectedDeviceChanged;
            Client.AudioDeviceManager.IsEnabledChanged += OnIsEnabledChanged;
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
            Client.AudioDeviceManager.SelectedDeviceChanged -= OnSelectedDeviceChanged;
            Client.AudioDeviceManager.IsEnabledChanged -= OnIsEnabledChanged;
            
            base.OnDestroying();
        }

        private void OnSelectedDeviceChanged(MicrophoneDeviceInfo previousDevice, MicrophoneDeviceInfo currentDevice) 
            => SelectDeviceWithoutNotify(currentDevice);
        
        private void OnIsEnabledChanged(bool isEnabled) => UpdateDeviceState(isEnabled);
    }
}