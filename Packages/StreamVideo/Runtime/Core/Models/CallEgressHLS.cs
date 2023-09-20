using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallEgressHLS : IStateLoadableFrom<EgressHLSResponseInternalDTO, CallEgressHLS>
    {
        public string PlaylistUrl { get; private set; }

        void IStateLoadableFrom<EgressHLSResponseInternalDTO, CallEgressHLS>.LoadFromDto(EgressHLSResponseInternalDTO dto, ICache cache)
        {
            PlaylistUrl = dto.PlaylistUrl;
        }
    }
}