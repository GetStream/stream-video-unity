using System;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.StatefulModels
{
    internal sealed class StreamVideoCallParticipant : StreamStatefulModelBase<StreamVideoCallParticipant>,
        IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>, IStreamVideoCallParticipant
    {
        #region State

        public DateTimeOffset JoinedAt { get; private set; }

        public string Role { get; private set; }

        public IStreamVideoUser User { get; set; }

        //StreamTodo: not sure if this a unique 
        public string UserSessionId { get; private set; }

        #endregion

        public StreamVideoCallParticipant(string uniqueId, ICacheRepository<StreamVideoCallParticipant> repository,
            IStatefulModelContext context)
            : base(uniqueId, repository, context)
        {
        }

        protected override string InternalUniqueId
        {
            get => UserSessionId;
            set => UserSessionId = value;
        }

        protected override StreamVideoCallParticipant Self => this;

        void IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>.UpdateFromDto(
            CallParticipantResponseInternalDTO dto, ICache cache)
        {
            JoinedAt = dto.JoinedAt;
            Role = dto.Role;
            User = cache.TryCreateOrUpdate(dto.User);
            UserSessionId = dto.UserSessionId;
        }
    }
}