using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core;
using TMPro;
using UnityEngine;

namespace StreamVideo.ExampleProject.UI.Devices
{
    /// <summary>
    /// Event handler for device toggled event
    /// </summary>
    public delegate void DeviceToggleHandler(bool isActive);

    /// <summary>
    /// Panel that displays media device (microphone or camera) dropdown to pick the active device and a button to toggle on/off state 
    /// </summary>
    public abstract class MediaDevicePanelBase<TDevice> : MonoBehaviour where TDevice : struct
    {
        /// <summary>
        /// Event handler for device changed event
        /// </summary>
        public delegate void DeviceChangeHandler(TDevice deviceName, bool isActive);
        
        public event DeviceChangeHandler DeviceChanged;
        public event DeviceToggleHandler DeviceToggled;

        public TDevice SelectedDevice { get; private set; }
        
        //StreamTodo: android has DeviceStatus: Enabled, Disabled, NotSelected
        public bool IsDeviceActive { get; private set; } = true;

        public void Init(IStreamVideoClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            
            UpdateDevicesDropdown(GetDevices());
        }

        public void SelectDeviceWithoutNotify(TDevice device)
        {
            var index = _devices.IndexOf(device);
            if (index == -1)
            {
                Debug.LogError($"Failed to find index for device: {device}");
                return;
            }

            _dropdown.SetValueWithoutNotify(index);
        }
        
        protected IStreamVideoClient Client { get; private set; }

        // Called by Unity
        protected void Awake()
        {
            _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            _deviceButton.Init(_buttonOnSprite, _buttonOffSprite);
            _deviceButton.Clicked += OnDeviceButtonClicked;
            
            _refreshDeviceInterval = new WaitForSeconds(0.5f);
            _refreshCoroutine = StartCoroutine(RefreshDevicesList());
        }

        // Called by Unity
        protected void Start()
        {
            _deviceButton.UpdateSprite(IsDeviceActive);
        }

        // Called by Unity
        protected void OnDestroy()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
            }
        }

        protected abstract IEnumerable<TDevice> GetDevices();

        protected abstract string GetDeviceName(TDevice device);

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
        private readonly List<TDevice> _devices = new List<TDevice>();

        private void OnDropdownValueChanged(int optionIndex)
        {
            var deviceName = _devices.ElementAt(optionIndex);
            if (deviceName.Equals(default))
            {
                Debug.LogError($"Failed to select device with index: {optionIndex}. Available devices: " +
                               string.Join(", ", _devices));
                return;
            }

            SelectedDevice = deviceName;

            DeviceChanged?.Invoke(SelectedDevice, IsDeviceActive);
        }

        private void OnDeviceButtonClicked()
        {
            IsDeviceActive = !IsDeviceActive;
            _deviceButton.UpdateSprite(IsDeviceActive);
            DeviceToggled?.Invoke(IsDeviceActive);
        }

        // User can add/remove devices any time so we must constantly monitor the devices list
        private IEnumerator RefreshDevicesList()
        {
            while (true)
            {
                while (Client == null)
                {
                    yield return _refreshDeviceInterval;
                }
                
                var availableDevices = GetDevices().ToList();
                var devicesChanged = !_devices.SequenceEqual(availableDevices);
                if (devicesChanged)
                {
                    var prevDevicesLog = string.Join(", ", _devices);
                    var newDevicesLog = string.Join(", ", availableDevices);
                    Debug.Log($"Device list changed. Previous: {prevDevicesLog}, Current: {newDevicesLog}");

                    UpdateDevicesDropdown(availableDevices);
                }

                yield return _refreshDeviceInterval;
            }
        }

        private void UpdateDevicesDropdown(IEnumerable<TDevice> devices)
        {
            _devices.Clear();
            _devices.AddRange(devices);

            _dropdown.ClearOptions();
            _dropdown.AddOptions(devices.Select(GetDeviceName).ToList());

            if (!EqualityComparer<TDevice>.Default.Equals(SelectedDevice, default) && !devices.Contains(SelectedDevice))
            {
                Debug.LogError($"Previously active device was unplugged: {SelectedDevice}");
                //StreamTodo: handle case when user unplugged active device
            }
        }
    }
}