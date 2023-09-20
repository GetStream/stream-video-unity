using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class ScreensharingSettings : IStateLoadableFrom<ScreensharingSettingsInternalDTO, ScreensharingSettings>
    {
        public bool AccessRequestEnabled { get; private set;}

        public bool Enabled { get; private set;}

        void IStateLoadableFrom<ScreensharingSettingsInternalDTO, ScreensharingSettings>.LoadFromDto(ScreensharingSettingsInternalDTO dto, ICache cache)
        {
            AccessRequestEnabled = dto.AccessRequestEnabled;
            Enabled = dto.Enabled;
        }
    }
}