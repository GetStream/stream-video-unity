using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class SFU : IStateLoadableFrom<SFUResponseInternalDTO, SFU>
    {
        public string EdgeName { get; private set; }

#if STREAM_LOCAL_SFU
        public string Url => StreamVideoLowLevelClient.LocalSfuWebSocketUri.ToString();
#else
                public string Url { get; private set; }
#endif

        public string WsEndpoint { get; private set; }

        void IStateLoadableFrom<SFUResponseInternalDTO, SFU>.LoadFromDto(SFUResponseInternalDTO dto, ICache cache)
        {
            EdgeName = dto.EdgeName;
#if !STREAM_LOCAL_SFU
            Url = dto.Url;
#endif
            WsEndpoint = dto.WsEndpoint;
        }
    }
}