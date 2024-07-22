using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class ScreensharingSettings : IStateLoadableFrom<ScreensharingSettingsResponseInternalDTO, ScreensharingSettings>
    {
        public bool AccessRequestEnabled { get; private set;}

        public bool Enabled { get; private set;}

        void IStateLoadableFrom<ScreensharingSettingsResponseInternalDTO, ScreensharingSettings>.LoadFromDto(ScreensharingSettingsResponseInternalDTO dto, ICache cache)
        {
            AccessRequestEnabled = dto.AccessRequestEnabled;
            Enabled = dto.Enabled;
        }
    }
}