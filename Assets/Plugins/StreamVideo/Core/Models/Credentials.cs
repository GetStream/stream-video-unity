using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class Credentials : IStateLoadableFrom<CredentialsInternalDTO, Credentials>
    {
        public IReadOnlyList<ICEServer> IceServers => _iceServers;

        public SFU Server { get; private set; }

        public string Token { get; private set; }

        void IStateLoadableFrom<CredentialsInternalDTO, Credentials>.LoadFromDto(CredentialsInternalDTO dto,
            ICache cache)
        {
            _iceServers.TryReplaceFromDtoCollection(dto.IceServers, cache);
            Server = cache.TryUpdateOrCreateFromDto(Server, dto.Server);
            Token = dto.Token;
        }
        
        private readonly List<ICEServer> _iceServers = new List<ICEServer>();
    }
}