namespace StreamVideo.Core.DeviceManagers
{
    /// <summary>
    /// Represents a Physical Camera Device that can potentially be activated to capture a video stream
    /// </summary>
    public readonly struct CameraDeviceInfo
    {
        public string Name { get; }
        public bool IsFrontFacing { get; }

        public CameraDeviceInfo(string name, bool isFrontFacing)
        {
            Name = name;
            IsFrontFacing = isFrontFacing;
        }
    }
}