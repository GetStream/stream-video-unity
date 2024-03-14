using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class BroadcastSettings : IStateLoadableFrom<BroadcastSettingsRequestInternalDTO, BroadcastSettings>,
        IStateLoadableFrom<BroadcastSettingsResponseInternalDTO, BroadcastSettings>
    {
        public bool Enabled { get; private set; }

        public HLSSettings Hls { get; private set; }

        void IStateLoadableFrom<BroadcastSettingsRequestInternalDTO, BroadcastSettings>.LoadFromDto(
            BroadcastSettingsRequestInternalDTO dto, ICache cache)
        {
            Enabled = dto.Enabled;
            Hls = cache.TryUpdateOrCreateFromDto(Hls, dto.Hls);
        }

        void IStateLoadableFrom<BroadcastSettingsResponseInternalDTO, BroadcastSettings>.LoadFromDto(
            BroadcastSettingsResponseInternalDTO dto, ICache cache)
        {
            Enabled = dto.Enabled;
            Hls = cache.TryUpdateOrCreateFromDto(Hls, dto.Hls);
        }
    }
}