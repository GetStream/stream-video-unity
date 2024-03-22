using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal abstract class DeviceManagerBase<TDeviceInfo> : IDeviceManager<TDeviceInfo>
    {
        public bool IsEnabled { get; private set; } = true;
        
        public abstract TDeviceInfo SelectedDevice { get; protected set; }

        public void Enable() => SetEnabled(true);

        public void Disable() => SetEnabled(false);

        public void SetEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
            OnSetEnabled(isEnabled);
        }

        public abstract IEnumerable<TDeviceInfo> EnumerateDevices();

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