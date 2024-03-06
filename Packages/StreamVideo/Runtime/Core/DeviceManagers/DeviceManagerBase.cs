using System;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal abstract class DeviceManagerBase : IDeviceManager
    {
        public bool IsEnabled { get; private set; } = true;

        internal DeviceManagerBase(RtcSession rtcSession)
        {
            RtcSession = rtcSession ?? throw new ArgumentNullException(nameof(rtcSession));
            
            //StreamTodo: react to when video & audio streams become available and disable them if IsEnabled was set to false before the call
        }

        public void Enable() => SetEnabled(true);

        public void Disable() => SetEnabled(false);

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            OnSetEnabled(isEnabled);
        }

        protected RtcSession RtcSession { get; }

        protected abstract void OnSetEnabled(bool isEnabled);
    }
}