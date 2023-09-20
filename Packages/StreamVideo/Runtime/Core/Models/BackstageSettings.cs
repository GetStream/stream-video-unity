using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class BackstageSettings : IStateLoadableFrom<BackstageSettingsInternalDTO, BackstageSettings>
    {
        public bool Enabled { get; private set;}

        void IStateLoadableFrom<BackstageSettingsInternalDTO, BackstageSettings>.LoadFromDto(BackstageSettingsInternalDTO dto, ICache cache)
        {
            Enabled = dto.Enabled;
        }
    }
}