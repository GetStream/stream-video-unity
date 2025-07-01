using System;
using StreamVideo.Libs.DeviceManagers;

namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Microphone Device that can potentially be activated to capture an audio stream
    /// </summary>
    public readonly struct MicrophoneDeviceInfo : IEquatable<MicrophoneDeviceInfo>
    {
        public string Name { get; }

        public NativeAudioDeviceManager.AudioDeviceInfo DeviceInfo { get; }
        public readonly bool IsUnityApiDevice;

        /// <summary>
        /// Legacy constructor for platforms on which native binding to audio devices is not yet supported, and we fall back to Unity's Audio API.
        /// </summary>
        /// <param name="name"></param>
        internal MicrophoneDeviceInfo(string name)
        {
            Name = name;
            DeviceInfo = default;
            IsUnityApiDevice = true;
        }

        internal MicrophoneDeviceInfo(NativeAudioDeviceManager.AudioDeviceInfo audioDeviceInfo)
        {
            Name = string.IsNullOrEmpty(audioDeviceInfo.FriendlyName)
                ? audioDeviceInfo.Name
                : audioDeviceInfo.FriendlyName;
            
            DeviceInfo = audioDeviceInfo;
            IsUnityApiDevice = false;
        }

        public bool Equals(MicrophoneDeviceInfo other)
        {
            if (IsUnityApiDevice)
            {
                return Name == other.Name;
            }

            return DeviceInfo.Equals(other.DeviceInfo);
        }

        public override bool Equals(object obj) => obj is MicrophoneDeviceInfo other && Equals(other);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);

        public static bool operator ==(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => left.Equals(right);

        public static bool operator !=(MicrophoneDeviceInfo left, MicrophoneDeviceInfo right) => !left.Equals(right);
        
        public override string ToString()
        {
            if (IsUnityApiDevice)
            {
                return DeviceInfo.IsValid ? DeviceInfo.ToString() : "None";
            }
            return string.IsNullOrEmpty(Name) ? "None" : Name;
        }

        internal bool IsValid => (IsUnityApiDevice && !string.IsNullOrEmpty(Name)) || (!IsUnityApiDevice && DeviceInfo.IsValid);
    }
}