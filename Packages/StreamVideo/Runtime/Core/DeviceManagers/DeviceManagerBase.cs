using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal abstract class DeviceManagerBase<TDeviceInfo> : IDeviceManager
    {
        public bool IsEnabled { get; private set; } = true;

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

        public abstract Task<bool> IsDeviceStreamingAsync(TDeviceInfo device);

        internal DeviceManagerBase(RtcSession rtcSession)
        {
            RtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            
            //StreamTodo: react to when video & audio streams become available and disable them if IsEnabled was set to false before the call
        }

        protected RtcSession RtcSession { get; }

        protected abstract void OnSetEnabled(bool isEnabled);
    }
}