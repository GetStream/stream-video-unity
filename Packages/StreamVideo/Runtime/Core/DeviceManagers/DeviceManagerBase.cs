using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    public delegate void
        SelectedDeviceChangeHandler<in TDeviceInfo>(TDeviceInfo previousDevice, TDeviceInfo currentDevice);

    internal abstract class DeviceManagerBase<TDeviceInfo> : IDeviceManager<TDeviceInfo>
    {
        public event SelectedDeviceChangeHandler<TDeviceInfo> SelectedDeviceChanged;

        public bool IsEnabled { get; private set; } = true;

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
            IsEnabled = isEnabled;
            OnSetEnabled(isEnabled);
        }

        public abstract IEnumerable<TDeviceInfo> EnumerateDevices();

        public Task<bool> TestDeviceAsync(TDeviceInfo device, float timeout = 0.2f)
        {
            if (timeout <= 0f || timeout > 20f)
            {
                throw new ArgumentOutOfRangeException(
                    $"'{nameof(timeout)}' argument must be between 0 and 20 seconds, given: {timeout}");
            }

            return OnTestDeviceAsync(device, (int)(timeout * 1000));
        }

        public void Dispose() => OnDisposing();

        internal DeviceManagerBase(RtcSession rtcSession, IInternalStreamVideoClient client)
        {
            RtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            Client = client ?? throw new ArgumentNullException(nameof(client));

            //StreamTodo: react to when video & audio streams become available and disable them if IsEnabled was set to false before the call
        }

        protected RtcSession RtcSession { get; }
        protected IInternalStreamVideoClient Client { get; }

        protected abstract void OnSetEnabled(bool isEnabled);

        protected abstract Task<bool> OnTestDeviceAsync(TDeviceInfo device, int msTimeout);

        protected virtual void OnDisposing()
        {
        }
        
        private TDeviceInfo _selectedDevice;
    }
}