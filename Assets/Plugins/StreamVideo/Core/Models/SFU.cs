using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class SFU : IStateLoadableFrom<SFUResponseInternalDTO, SFU>
    {
        public string EdgeName { get; private set; }

        public string Url { get; private set; }

        public string WsEndpoint { get; private set; }

        void IStateLoadableFrom<SFUResponseInternalDTO, SFU>.LoadFromDto(SFUResponseInternalDTO dto, ICache cache)
        {
            EdgeName = dto.EdgeName;
            Url = dto.Url;
            WsEndpoint = dto.WsEndpoint;
        }
    }
}