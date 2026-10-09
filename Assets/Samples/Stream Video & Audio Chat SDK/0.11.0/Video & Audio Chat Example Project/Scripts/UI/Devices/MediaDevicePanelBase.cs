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
    /// Panel that displays media device (microphone or camera) dropdown to pick the active device and a button to toggle on/off state 
    /// </summary>
    public abstract class MediaDevicePanelBase<TDevice> : MonoBehaviour 
        where TDevice : struct
    {
        public void Init(IStreamVideoClient client, UIManager uiManager)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            UIManager = uiManager ? uiManager : throw new ArgumentNullException(nameof(uiManager));

            UpdateDevicesDropdown(GetDevices());

            InitSelf();
            
            OnInit();
        }

        public void SelectDeviceWithoutNotify(TDevice device)
        {
            var index = _devices.IndexOf(device);
            if (index == -1)
            {
                // The list can be stale, e.g. devices become visible only after camera permission is granted
                UpdateDevicesDropdown(GetDevices().ToList());
                index = _devices.IndexOf(device);
            }

            if (index == -1)
            {
                Debug.LogError($"Failed to find index for device: {GetDeviceName(device)}. Available devices: " +
                               DescribeDevices(_devices));
                return;
            }

            _dropdown.SetValueWithoutNotify(index);
        }

        /// <summary>
        /// Called when parent screen is shown.
        /// </summary>
        public void NotifyParentShow()
        {
            _deviceButton.UpdateSprite(IsDeviceEnabled);
            OnParentShow();
        }

        /// <summary>
        /// Called when parent screen is hidden
        /// </summary>
        public void NotifyParentHide()
        {
            OnParentHide();
        }
        
        protected IStreamVideoClient Client { get; private set; }

        // Called by Unity
        protected void Awake()
        {
            _refreshCoroutine = StartCoroutine(RefreshDevicesList());
        }

        // Called by Unity
        protected void Start()
        {
            _deviceButton.UpdateSprite(IsDeviceEnabled);
        }

        // Called by Unity
        protected void OnDestroy()
        {
            OnDestroying();
            
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
            }
        }
        
        protected virtual void OnInit()
        {
            
        }

        protected virtual void OnDestroying()
        {
            
        }

        protected abstract IEnumerable<TDevice> GetDevices();
        protected abstract TDevice SelectedDevice { get; }
        protected abstract bool IsDeviceEnabled { get; set; }
        protected UIManager UIManager { get; private set; }

        protected abstract string GetDeviceName(TDevice device);

        protected abstract void ChangeDevice(TDevice device);

        protected virtual void OnParentShow()
        {
            
        }
        
        protected virtual void OnParentHide()
        {
            
        }
        
        protected void UpdateDeviceState(bool isEnabled)
        {
            // Update UI first to reflect the change immediately
            _deviceButton.UpdateSprite(isEnabled);
            IsDeviceEnabled = isEnabled;
        }

        private readonly List<TDevice> _devices = new List<TDevice>();
        
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
        
        private void InitSelf()
        {
            _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

            _deviceButton.Init(_buttonOnSprite, _buttonOffSprite);
            _deviceButton.Clicked += OnDeviceButtonClicked;
            
            _refreshDeviceInterval = new WaitForSeconds(0.5f);
        }

        private void OnDropdownValueChanged(int optionIndex)
        {
            var device = _devices.ElementAt(optionIndex);
            if (device.Equals(default))
            {
                Debug.LogError($"Failed to select device with index: {optionIndex}. Available devices: " +
                               DescribeDevices(_devices));
                return;
            }

            ChangeDevice(device);
        }

        private void OnDeviceButtonClicked()
        {
            var newState = !IsDeviceEnabled;

            UpdateDeviceState(newState);
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
                    var prevDevicesLog = DescribeDevices(_devices);
                    var newDevicesLog = DescribeDevices(availableDevices);
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
                Debug.LogError($"Previously active device was unplugged: {GetDeviceName(SelectedDevice)}. Devices: " + DescribeDevices(devices));
                //StreamTodo: handle case when user unplugged active device
            }
        }

        // Avoids string.Join<TDevice>: IL2CPP on iOS crashed in its shared generic code for device structs
        private string DescribeDevices(IEnumerable<TDevice> devices)
        {
            var names = new List<string>();
            foreach (var device in devices)
            {
                names.Add(GetDeviceName(device));
            }

            return string.Join(", ", names);
        }
    }
}