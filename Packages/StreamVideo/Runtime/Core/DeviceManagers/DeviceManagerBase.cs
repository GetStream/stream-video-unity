﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Libs.Logs;

namespace StreamVideo.Core.DeviceManagers
{
    public delegate void DeviceEnabledChangeHandler(bool isEnabled);

    public delegate void
        SelectedDeviceChangeHandler<in TDeviceInfo>(TDeviceInfo previousDevice, TDeviceInfo currentDevice);

    internal abstract class DeviceManagerBase<TDeviceInfo> : IDeviceManager<TDeviceInfo> where TDeviceInfo : struct
    {
        public event DeviceEnabledChangeHandler IsEnabledChanged;

        public event SelectedDeviceChangeHandler<TDeviceInfo> SelectedDeviceChanged;

        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                if (value == _isEnabled)
                {
                    return;
                }

                _isEnabled = value;
                IsEnabledChanged?.Invoke(IsEnabled);
            }
        }

        public TDeviceInfo SelectedDevice
        {
            get => _selectedDevice;
            protected set
            {
                if (EqualityComparer<TDeviceInfo>.Default.Equals(value, _selectedDevice))
                {
                    return;
                }

                var prev = _selectedDevice;
                _selectedDevice = value;
                SelectedDeviceChanged?.Invoke(prev, value);
            }
        }

        public void Enable() => SetEnabled(true);

        public void Disable() => SetEnabled(false);

        public void SetEnabled(bool isEnabled)
        {
            if (IsEnabled == isEnabled)
            {
                return;
            }

            IsEnabled = isEnabled;
            OnSetEnabled(isEnabled);
        }

        public abstract IEnumerable<TDeviceInfo> EnumerateDevices();

        public Task<bool> TestDeviceAsync(TDeviceInfo device, float timeout = 1f)
        {
            const float MinTimeout = 0f;
            const float MaxTimeout = 20f;

            if (timeout <= MinTimeout || timeout > MaxTimeout)
            {
                throw new ArgumentOutOfRangeException(
                    $"'{nameof(timeout)}' argument must be between {MinTimeout} and {MaxTimeout} seconds, given: {timeout}");
            }

            return OnTestDeviceAsync(device, (int)(timeout * 1000));
        }

        // StreamTODO: add filter option. E.g. so we can easily consider only front cameras on ios/android
        public async Task<TDeviceInfo?> TryFindFirstWorkingDeviceAsync(float testTimeoutPerDevice = 1f)
        {
            foreach (var device in EnumerateDevices())
            {
                var isWorking = await TestDeviceAsync(device, testTimeoutPerDevice);
                if (isWorking)
                {
                    return device;
                }
            }

            return null;
        }

        public void Dispose() => OnDisposing();

        internal DeviceManagerBase(RtcSession rtcSession, IInternalStreamVideoClient client, ILogs logs)
        {
            RtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Logs = logs ?? throw new ArgumentNullException(nameof(logs));

            //StreamTodo: react to when video & audio streams become available and disable them if IsEnabled was set to false before the call
        }

        internal void Update() => OnUpdate();

        protected RtcSession RtcSession { get; }
        protected IInternalStreamVideoClient Client { get; }
        protected ILogs Logs { get; }

        protected abstract void OnSetEnabled(bool isEnabled);

        protected abstract Task<bool> OnTestDeviceAsync(TDeviceInfo device, int msTimeout);

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnDisposing()
        {
        }

        private TDeviceInfo _selectedDevice;
        private bool _isEnabled;
    }
}