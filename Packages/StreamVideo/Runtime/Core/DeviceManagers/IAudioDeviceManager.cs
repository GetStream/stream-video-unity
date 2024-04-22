namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Manages interactions with audio recording devices (Microphones).
    /// </summary>
    public interface IAudioDeviceManager : IDeviceManager<MicrophoneDeviceInfo>
    {
        /// <summary>
        /// Select a microphone device for audio capturing.
        /// </summary>
        /// <param name="device">Device to select</param>
        /// <param name="enable">Enable this device (Start Capturing Audio)</param>
        void SelectDevice(MicrophoneDeviceInfo device, bool enable);
    }
}