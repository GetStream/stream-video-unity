using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    /// <summary>
    /// Panel that displays media device (microphone or camera) dropdown to pick the active device and a button to toggle on/off state 
    /// </summary>
    public abstract class MediaDevicePanelBase : MonoBehaviour
    {
        public event Action DeviceChanged;
        public event Action DeviceToggled;

        public string SelectedDeviceName { get; private set; }
        public bool IsDeviceActive { get; private set; } = true;

        public void SelectDeviceWithoutNotify(string deviceName)
        {
            var index = _deviceNames.IndexOf(deviceName);
            if (index == -1)
            {
                Debug.LogError($"Failed to find index for device: {deviceName}");
                return;
            }

            _dropdown.SetValueWithoutNotify(index);
        }

        protected void Awake()
        {
            _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            _deviceButton.Init(_buttonOnSprite, _buttonOffSprite);
            _deviceButton.UpdateSprite(IsDeviceActive);
            _deviceButton.Clicked += OnDeviceButtonClicked;

            UpdateDevicesDropdown(GetDevicesNames().ToList());
            
            _refreshDeviceInterval = new WaitForSeconds(0.5f);
            _refreshCoroutine = StartCoroutine(RefreshDevicesList());
        }

        protected void OnDestroy()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
            }
        }

        protected abstract IEnumerable<string> GetDevicesNames();

        [SerializeField]
        private Sprite _buttonOnSprite;

        [SerializeField]
        private Sprite _buttonOffSprite;

        [SerializeField]
        private MediaDeviceButton _deviceButton;

        [SerializeField]
        private TMP_Dropdown _dropdown;

        private Coroutine _refreshCoroutine;
        private YieldInstruction _refreshDeviceInterval;
        private readonly List<string> _deviceNames = new List<string>();

        private void OnDropdownValueChanged(int optionIndex)
        {
            var deviceName = _deviceNames.ElementAt(optionIndex);
            if (deviceName == null)
            {
                Debug.LogError($"Failed to select device with index: {optionIndex}. Available devices: " +
                               string.Join(", ", _deviceNames));
                return;
            }

            SelectedDeviceName = deviceName;

            DeviceChanged?.Invoke();
        }

        private void OnDeviceButtonClicked()
        {
            IsDeviceActive = !IsDeviceActive;
            _deviceButton.UpdateSprite(IsDeviceActive);
            DeviceToggled?.Invoke();
        }

        // User can add/remove devices any time so we must constantly monitor the devices list
        private IEnumerator RefreshDevicesList()
        {
            while (true)
            {
                var availableDevices = GetDevicesNames().ToList();
                var devicesChanged = !_deviceNames.SequenceEqual(availableDevices);
                if (devicesChanged)
                {
                    var prevDevicesLog = string.Join(", ", _deviceNames);
                    var newDevicesLog = string.Join(", ", availableDevices);
                    Debug.Log($"Device list changed. Previous: {prevDevicesLog}, Current: {newDevicesLog}");

                    UpdateDevicesDropdown(availableDevices);
                }

                yield return _refreshDeviceInterval;
            }
        }

        private void UpdateDevicesDropdown(List<string> devices)
        {
            _deviceNames.Clear();
            _deviceNames.AddRange(devices);

            _dropdown.ClearOptions();
            _dropdown.AddOptions(devices);

            if (!string.IsNullOrEmpty(SelectedDeviceName) && !devices.Contains(SelectedDeviceName))
            {
                Debug.LogError($"Previously active device was unplugged: {SelectedDeviceName}");
                //StreamTodo: handle case when user unplugged active device
            }
        }
    }
}