using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreamVideo.Core.DeviceManagers
{
    public interface IDeviceManager<TDeviceInfo> : IDisposable where TDeviceInfo : struct
    {
        /// <summary>
        /// Event triggered when the <see cref="SelectedDevice"/> changes.
        /// </summary>
        event SelectedDeviceChangeHandler<TDeviceInfo> SelectedDeviceChanged;

        /// <summary>
        /// Is device enabled. Enabled device will stream output during the call.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Currently selected device. This device will be used to capture data.
        /// </summary>
        TDeviceInfo SelectedDevice { get; }

        /// <summary>
        /// START capturing data from the <see cref="SelectedDevice"/>.
        /// </summary>
        void Enable();

        /// <summary>
        /// STOP capturing data from the <see cref="SelectedDevice"/>
        /// </summary>
        void Disable();

        /// <summary>
        /// Set enabled state for this device.
        /// </summary>
        void SetEnabled(bool isEnabled);

        /// <summary>
        /// Enumerate all available devices. This list contains all devices exposed by the underlying OS.
        /// </summary>
        IEnumerable<TDeviceInfo> EnumerateDevices();

        /// <summary>
        /// Check if the device is capturing any data.
        /// This can be useful when there are multiple devices available and you want to filter out the ones that actually work.
        /// For example, on Windows/Mac/Linux there can be many virtual cameras/microphones available that are not capturing any data.
        /// You typically want to present all available devices to users but you may want to show working devices first or enable the first working device by default.
        /// </summary>
        /// <param name="device">Device to test. You can obtain them from <see cref="DeviceManagerBase{TDeviceInfo}.EnumerateDevices"/></param>
        /// <param name="timeout">How long the test will wait for camera input. Please not that depending on OS and the device there can be delay in starting the device. Timeout below 0.5 seconds can not be enough for some device.</param>
        /// <returns>True if device is providing captured data</returns>
        Task<bool> TestDeviceAsync(TDeviceInfo device, float timeout = 1f);

        /// <summary>
        /// Iterates over all available devices and performs <see cref="TestDeviceAsync"/> on each until the first working device is found
        /// </summary>
        /// <param name="testTimeoutPerDevice"></param>
        /// <returns>First found working device of NULL if none of the devices worked</returns>
        Task<TDeviceInfo?> TryFindFirstWorkingDeviceAsync(float testTimeoutPerDevice = 1f);
    }
}