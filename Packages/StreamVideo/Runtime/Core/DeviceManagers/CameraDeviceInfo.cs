using System.Threading.Tasks;

namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Physical Camera Device that can potentially be activated to capture a video stream
    /// </summary>
    public readonly struct CameraDeviceInfo
    {
        public string Name { get; }
        public bool IsFrontFacing { get; }

        public CameraDeviceInfo(string name, bool isFrontFacing, IVideoDeviceManager videoDeviceManager)
        {
            _videoDeviceManager = videoDeviceManager;
            Name = name;
            IsFrontFacing = isFrontFacing;
        }

        public Task<bool> TestDeviceAsync() => _videoDeviceManager.TestDeviceAsync(this);

        private readonly IVideoDeviceManager _videoDeviceManager;
    }
}