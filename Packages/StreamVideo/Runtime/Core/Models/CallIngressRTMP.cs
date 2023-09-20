using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallIngressRTMP : IStateLoadableFrom<RTMPIngressInternalDTO, CallIngressRTMP>
    {
        public string Address { get; private set; }

        void IStateLoadableFrom<RTMPIngressInternalDTO, CallIngressRTMP>.LoadFromDto(RTMPIngressInternalDTO dto, ICache cache)
        {
            Address = dto.Address;
        }
    }
}