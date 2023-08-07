using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.Models
{
    //StreamTodo: this should probably be a stateful model but we need a unique identifier for that
    public sealed class CallMember: IStateLoadableFrom<MemberResponseInternalDTO, CallMember>
    {
        /// <summary>
        /// Date/time of creation
        /// </summary>
        public System.DateTimeOffset CreatedAt { get; private set;}

        /// <summary>
        /// Custom member response data
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Custom { get; private set;}

        /// <summary>
        /// Date/time of deletion
        /// </summary>
        public System.DateTimeOffset DeletedAt { get; private set;}

        public string Role { get; private set;}

        /// <summary>
        /// Date/time of the last update
        /// </summary>
        public System.DateTimeOffset UpdatedAt { get; private set;}

        public IStreamVideoUser User { get; private set;}

        public string UserId { get; private set;}

        void IStateLoadableFrom<MemberResponseInternalDTO, CallMember>.LoadFromDto(MemberResponseInternalDTO dto, ICache cache)
        {
            CreatedAt = dto.CreatedAt;
            // StreamTodo: This is normally tied to stateful model instance
            Custom = dto.Custom;
            DeletedAt = dto.DeletedAt;
            Role = dto.Role;
            UpdatedAt = dto.UpdatedAt;
            User = cache.TryCreateOrUpdate(dto.User);
            UserId = dto.UserId;
        }
    }
}