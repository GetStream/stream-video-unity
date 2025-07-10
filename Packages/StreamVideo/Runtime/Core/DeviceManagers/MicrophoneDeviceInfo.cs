using System;

namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Microphone Device that can potentially be activated to capture an audio stream
    /// </summary>
    public readonly struct MicrophoneDeviceInfo : IEquatable<MicrophoneDeviceInfo>
    {
        internal int? IntId { get; }
        internal string StringId { get; }

        public string Name { get; }

        public readonly bool UseUnityAudioSystem;

        /// <summary>
        /// Legacy constructor for platforms on which native binding to audio devices is not yet supported, and we fall back to Unity's Audio API.
        /// </summary>
        /// <param name="name"></param>
        internal MicrophoneDeviceInfo(string name)
        {
            IntId = default;
            StringId = Name = name;
            UseUnityAudioSystem = true;
        }

        internal MicrophoneDeviceInfo(int id, string name)
        {
            IntId = id;
            Name = name;
            StringId = string.Empty;
            UseUnityAudioSystem = false;
        }

        public bool Equals(MicrophoneDeviceInfo other)
        {
            if (UseUnityAudioSystem)
            {
                return string.Equals(StringId, other.StringId);
            }

            return IntId == other.IntId;
        }

        public override bool Equals(object obj) => obj is MicrophoneDeviceInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                if (UseUnityAudioSystem)
                {
                    return (UseUnityAudioSystem.GetHashCode() * 397) ^ (StringId?.GetHashCode() ?? 0);
                }

                return (UseUnityAudioSystem.GetHashCode() * 397) ^ (IntId?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => left.Equals(right);

        public static bool operator !=(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => !left.Equals(right);

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "Invalid" : Name;
        }

        public bool IsValid => UseUnityAudioSystem ? !string.IsNullOrEmpty(StringId) : IntId.HasValue;
    }
}