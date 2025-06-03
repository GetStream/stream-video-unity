using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Models
{
    public sealed class CallEvent : IStateLoadableFrom<SendCallEventRequestInternalDTO, CallEvent>
    {
        public IReadOnlyDictionary<string, object> Custom => _custom;
        
        void IStateLoadableFrom<SendCallEventRequestInternalDTO, CallEvent>.LoadFromDto(SendCallEventRequestInternalDTO dto, ICache cache)
        {
            foreach (var pair in dto.Custom)
            {
                _custom.Add(pair.Key, pair.Value);
            }
        }
        
        private readonly Dictionary<string, object> _custom = new Dictionary<string, object>();

    }
}