using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallEgressRTMP : IStateLoadableFrom<EgressRTMPResponseInternalDTO, CallEgressRTMP>
    {
        public string Name { get; private set; }

        public string StreamKey { get; private set; }

        void IStateLoadableFrom<EgressRTMPResponseInternalDTO, CallEgressRTMP>.LoadFromDto(EgressRTMPResponseInternalDTO dto, ICache cache)
        {
            Name = dto.Name;
            StreamKey = dto.StreamKey;
        }
    }
}