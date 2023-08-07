using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class BroadcastSettings : IStateLoadableFrom<BroadcastSettingsInternalDTO, BroadcastSettings>
    {
        public bool Enabled { get; private set;}

        public HLSSettings Hls { get; private set;}

        void IStateLoadableFrom<BroadcastSettingsInternalDTO, BroadcastSettings>.LoadFromDto(BroadcastSettingsInternalDTO dto, ICache cache)
        {
            Enabled = dto.Enabled;
            Hls = cache.TryUpdateOrCreateFromDto(Hls, dto.Hls);
        }
    }
}