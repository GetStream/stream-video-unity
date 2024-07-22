using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class BackstageSettings : IStateLoadableFrom<BackstageSettingsResponseInternalDTO, BackstageSettings>
    {
        public bool Enabled { get; private set;}

        void IStateLoadableFrom<BackstageSettingsResponseInternalDTO, BackstageSettings>.LoadFromDto(BackstageSettingsResponseInternalDTO dto, ICache cache)
        {
            Enabled = dto.Enabled;
        }
    }
}