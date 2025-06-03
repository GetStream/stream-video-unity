using System;
using System.Collections.Generic;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.Models
{
    public sealed class Reaction : IStateLoadableFrom<ReactionResponseInternalDTO, Reaction>
    {
        /// <summary>
        /// Custom data sent with the reaction. 
        /// </summary>
        public IReadOnlyDictionary<string, object> Custom => _custom;

        public string EmojiCode { get; private set; }

        public string Type { get; private set; }

        public IStreamVideoUser User { get; private set; }
        
        public DateTimeOffset CreatedAt { get; private set; }

        void IStateLoadableFrom<ReactionResponseInternalDTO, Reaction>.LoadFromDto(ReactionResponseInternalDTO dto, ICache cache)
        {
            foreach (var pair in dto.Custom)
            {
                _custom.Add(pair.Key, pair.Value);
            }
            EmojiCode = dto.EmojiCode;
            Type = dto.Type;
            User = cache.TryCreateOrUpdate(dto.User);
            CreatedAt = DateTimeOffset.UtcNow;
        }
        
        private readonly Dictionary<string, object> _custom = new Dictionary<string, object>();
    }
}