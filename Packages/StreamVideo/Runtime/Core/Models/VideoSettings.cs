using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class VideoSettings : IStateLoadableFrom<VideoSettingsInternalDTO, VideoSettings>
    {
        public bool AccessRequestEnabled { get; private set;}

        public bool CameraDefaultOn { get; private set;}

        public VideoSettingsCameraFacing CameraFacing { get; private set;}

        public bool Enabled { get; private set;}

        public TargetResolution TargetResolution { get; private set;}

        void IStateLoadableFrom<VideoSettingsInternalDTO, VideoSettings>.LoadFromDto(VideoSettingsInternalDTO dto, ICache cache)
        {
            AccessRequestEnabled = dto.AccessRequestEnabled;
            CameraDefaultOn = dto.CameraDefaultOn;
            CameraFacing = dto.CameraFacing.ToPublicEnum();
            Enabled = dto.Enabled;
            TargetResolution = cache.TryUpdateOrCreateFromDto(TargetResolution, dto.TargetResolution);
        }
    }
}