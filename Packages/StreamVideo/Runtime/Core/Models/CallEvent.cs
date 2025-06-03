using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.Models
{
    public sealed class CallEvent : IStateLoadableFrom<CustomVideoEventInternalDTO, CallEvent>
    {
        public IReadOnlyDictionary<string, object> Custom => _custom;
        
        public string CallCid { get; private set; }

        public System.DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Event sender
        /// </summary>
        public IStreamVideoUser User { get; private set; }
        
        void IStateLoadableFrom<CustomVideoEventInternalDTO, CallEvent>.LoadFromDto(CustomVideoEventInternalDTO dto, ICache cache)
        {
            foreach (var pair in dto.Custom)
            {
                _custom.Add(pair.Key, pair.Value);
            }
            
            CallCid = dto.CallCid;
            CreatedAt = dto.CreatedAt;
            User = cache.TryCreateOrUpdate(dto.User);
        }
        
        private readonly Dictionary<string, object> _custom = new Dictionary<string, object>();

    }
}