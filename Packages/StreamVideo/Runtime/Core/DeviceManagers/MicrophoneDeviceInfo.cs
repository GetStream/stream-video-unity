using System;

namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Microphone Device that can potentially be activated to capture an audio stream
    /// </summary>
    public readonly struct MicrophoneDeviceInfo : IEquatable<MicrophoneDeviceInfo>
    {
        public string Name { get; }

        public MicrophoneDeviceInfo(string name)
        {
            Name = name;
        }

        public bool Equals(MicrophoneDeviceInfo other) => Name == other.Name;

        public override bool Equals(object obj) => obj is MicrophoneDeviceInfo other && Equals(other);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);

        public static bool operator ==(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => left.Equals(right);

        public static bool operator !=(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => !left.Equals(right);

        internal bool IsValid => !string.IsNullOrEmpty(Name);
    }
}