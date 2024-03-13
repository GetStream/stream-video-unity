using System;

namespace StreamVideo.Core.DeviceManagers
{
    public interface IDeviceManager : IDisposable
    {
        /// <summary>
        /// Is device enabled. Enabled device will stream output during the call.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enable this device. Enabled device will stream output during the call.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disable this device. This works like "mute" and stops streaming output during the call until you enable this again. 
        /// </summary>
        void Disable();

        /// <summary>
        /// Set enabled state for this device.
        /// </summary>
        void SetEnabled(bool isEnabled);
    }
}