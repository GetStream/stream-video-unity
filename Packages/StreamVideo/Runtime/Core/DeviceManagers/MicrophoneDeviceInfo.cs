namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Microphone Device that can potentially be activated to capture an audio stream
    /// </summary>
    public readonly struct MicrophoneDeviceInfo
    {
        public string Name { get; }

        public MicrophoneDeviceInfo(string name)
        {
            Name = name;
        }
    }
}