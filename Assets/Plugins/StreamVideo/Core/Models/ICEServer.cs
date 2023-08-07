using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.Utils;

namespace StreamVideo.Core.Models
{
    public sealed class ICEServer : IStateLoadableFrom<ICEServerInternalDTO, ICEServer>
    {
        public string Password { get; private set; }

        public IReadOnlyList<string> Urls => _urls;

        public string Username { get; private set; }

        void IStateLoadableFrom<ICEServerInternalDTO, ICEServer>.LoadFromDto(ICEServerInternalDTO dto, ICache cache)
        {
            Password = dto.Password;
            _urls.TryReplaceValuesFromDto(dto.Urls);
            Username = dto.Username;
        }
        
        private readonly List<string> _urls = new List<string>();
    }
}