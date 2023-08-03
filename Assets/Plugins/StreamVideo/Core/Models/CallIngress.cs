using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallIngress : IStateLoadableFrom<CallIngressResponseInternalDTO, CallIngress>
    {
        public RTMPIngress Rtmp { get; private set; }

        CallIngress IStateLoadableFrom<CallIngressResponseInternalDTO, CallIngress>.LoadFromDto(CallIngressResponseInternalDTO dto, ICache cache)
        {
            Rtmp = Rtmp.TryLoadFromDto(dto.Rtmp, cache);

            return this;
        }
    }
}