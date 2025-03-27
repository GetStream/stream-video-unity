using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class RingSettings : IStateLoadableFrom<RingSettingsResponseInternalDTO, RingSettings>
    {
        public int AutoCancelTimeoutMs { get; private set;}

        public int IncomingCallTimeoutMs { get; private set;}

        void IStateLoadableFrom<RingSettingsResponseInternalDTO, RingSettings>.LoadFromDto(RingSettingsResponseInternalDTO dto, ICache cache)
        {
            AutoCancelTimeoutMs = dto.AutoCancelTimeoutMs;
            IncomingCallTimeoutMs = dto.IncomingCallTimeoutMs;
        }
    }
}