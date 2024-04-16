using System;
using System.Threading.Tasks;

namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Physical Camera Device that can potentially be activated to capture a video stream
    /// </summary>
    public readonly struct CameraDeviceInfo : IEquatable<CameraDeviceInfo>
    {
        public string Name { get; }
        public bool IsFrontFacing { get; }

        public CameraDeviceInfo(string name, bool isFrontFacing, IVideoDeviceManager videoDeviceManager)
        {
            _videoDeviceManager = videoDeviceManager;
            Name = name;
            IsFrontFacing = isFrontFacing;
        }

        public bool Equals(CameraDeviceInfo other) => Name == other.Name;

        public override bool Equals(object obj) => obj is CameraDeviceInfo other && Equals(other);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);

        public static bool operator ==(CameraDeviceInfo left, CameraDeviceInfo right) => left.Equals(right);

        public static bool operator !=(CameraDeviceInfo left, CameraDeviceInfo right) => !left.Equals(right);

        public Task<bool> TestDeviceAsync() => _videoDeviceManager.TestDeviceAsync(this);

        public override string ToString() => $"Camera Device - {Name}";

        internal bool IsValid => !string.IsNullOrEmpty(Name);

        private readonly IVideoDeviceManager _videoDeviceManager;
    }
}