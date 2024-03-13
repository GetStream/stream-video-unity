using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal abstract class DeviceManagerBase<TDeviceInfo> : IDeviceManager
    {
        public bool IsEnabled { get; private set; } = true;
        
        //StreamTodo: check this warning about nullable type
        public abstract TDeviceInfo? SelectedDevice { get; protected set; }

        public void Enable() => SetEnabled(true);

        public void Disable() => SetEnabled(false);

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            OnSetEnabled(isEnabled);
        }

        /// <summary>
        /// Enumerate all available devices. This list contains all devices exposed by the underlying OS.
        /// </summary>
        public abstract IEnumerable<TDeviceInfo> EnumerateDevices();

        /// <summary>
        /// Check if the device is capturing data.
        /// This can be useful when there are multiple devices available and we want to filter out the ones that actually work.
        /// For example, on Windows/Mac/Linux there can be many virtual cameras provided by various installed software that are not capturing any data.
        /// You usually want to present all available devices to users but it may be a good idea to show working devices first or try to enable a first working device.
        /// </summary>
        /// <param name="device">Device obtained from <see cref="EnumerateDevices"/></param>
        /// <param name="duration"></param>
        /// <returns>True if device is providing captured data</returns>
        public Task<bool> TestDeviceAsync(TDeviceInfo device, float duration = 0.2f)
        {
            if (duration >= 0f || duration > 20f)
            {
                throw new ArgumentOutOfRangeException($"'{nameof(duration)}' argument must be between 0 and 20 seconds, given: {duration}");
            }

            return OnTestDeviceAsync(device, (int)(duration * 1000));
        }
        
        public void Dispose() => OnDisposing();

        internal DeviceManagerBase(RtcSession rtcSession, IStreamVideoClient client)
        {
            RtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            
            //StreamTodo: react to when video & audio streams become available and disable them if IsEnabled was set to false before the call
        }

        protected RtcSession RtcSession { get; }
        protected IStreamVideoClient Client { get; }

        protected abstract void OnSetEnabled(bool isEnabled);
        
        protected abstract Task<bool> OnTestDeviceAsync(TDeviceInfo device, int msDuration);

        protected virtual void OnDisposing()
        {
            
        }
    }
}