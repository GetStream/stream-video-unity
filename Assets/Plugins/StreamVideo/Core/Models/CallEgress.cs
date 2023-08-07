using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallEgress : IStateLoadableFrom<EgressResponseInternalDTO, CallEgress>
    {
        public bool Broadcasting { get; private set; }

        public CallEgressHLS Hls { get; private set; }

        public IReadOnlyList<CallEgressRTMP> Rtmps => rtmps;

        void IStateLoadableFrom<EgressResponseInternalDTO, CallEgress>.LoadFromDto(EgressResponseInternalDTO dto, ICache cache)
        {
            Broadcasting = dto.Broadcasting;
            Hls = cache.TryUpdateOrCreateFromDto(Hls, dto.Hls);
            rtmps.ReplaceFromDtoCollection(dto.Rtmps, cache);
        }

        private readonly List<CallEgressRTMP> rtmps = new List<CallEgressRTMP>();
    }
}