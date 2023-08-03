using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class RTMPIngress : IStateLoadableFrom<RTMPIngressInternalDTO, RTMPIngress>
    {
        public string Address { get; set; } = default!;

        RTMPIngress IStateLoadableFrom<RTMPIngressInternalDTO, RTMPIngress>.LoadFromDto(RTMPIngressInternalDTO dto, ICache cache)
        {
            Address = dto.Address;

            return this;
        }
    }
}