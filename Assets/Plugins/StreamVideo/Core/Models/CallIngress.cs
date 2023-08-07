using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallIngress : IStateLoadableFrom<CallIngressResponseInternalDTO, CallIngress>
    {
        public CallIngressRTMP Rtmp { get; private set; }

        void IStateLoadableFrom<CallIngressResponseInternalDTO, CallIngress>.LoadFromDto(CallIngressResponseInternalDTO dto, ICache cache)
        {
            Rtmp = cache.TryUpdateOrCreateFromDto(Rtmp, dto.Rtmp);
        }
    }
}