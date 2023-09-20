using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class RingSettings : IStateLoadableFrom<RingSettingsInternalDTO, RingSettings>
    {
        public int AutoCancelTimeoutMs { get; private set;}

        public int IncomingCallTimeoutMs { get; private set;}

        void IStateLoadableFrom<RingSettingsInternalDTO, RingSettings>.LoadFromDto(RingSettingsInternalDTO dto, ICache cache)
        {
            AutoCancelTimeoutMs = dto.AutoCancelTimeoutMs;
            IncomingCallTimeoutMs = dto.IncomingCallTimeoutMs;
        }
    }
}