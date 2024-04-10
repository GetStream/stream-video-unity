using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreamVideo.Core.DeviceManagers
{
    public interface IDeviceManager<TDeviceInfo> : IDisposable
    {
        /// <summary>
        /// Is device enabled. Enabled device will stream output during the call.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Currently selected device. This device will be used to capture data.
        /// </summary>
        public TDeviceInfo SelectedDevice { get; }

        /// <summary>
        /// START capturing data from the <see cref="SelectedDevice"/>. Before calling this method you must first use <see cref="SetEnabled"/>
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
        /// Check if the device is capturing data.
        /// This can be useful when there are multiple devices available and you want to filter out the ones that actually work.
        /// For example, on Windows/Mac/Linux there can be many virtual cameras that are not capturing any data.
        /// You usually want to show all available devices to users but it may be a good idea to show working devices first or enable a first working device by default.
        /// </summary>
        /// <param name="device">Device to test. You can obtain them from <see cref="DeviceManagerBase{TDeviceInfo}.EnumerateDevices"/></param>
        /// <param name="timeout">How long the test will wait for camera input.</param>
        /// <returns>True if device is providing captured data</returns>
        Task<bool> TestDeviceAsync(TDeviceInfo device, float timeout = 0.2f);
    }
}