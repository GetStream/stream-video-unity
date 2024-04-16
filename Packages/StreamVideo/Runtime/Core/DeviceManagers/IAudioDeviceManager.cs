namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Manages interactions with audio recording devices (Microphones).
    /// </summary>
    public interface IAudioDeviceManager : IDeviceManager<MicrophoneDeviceInfo>
    {
        void SelectDevice(MicrophoneDeviceInfo device);
    }
}