using System;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.StatefulModels
{
    // StreamTodo: perhaps participant and member are not stateful models, they simply wrap a stateful model. If they'd represent standalone models they wouldn't rely on user.ID 
    internal sealed class StreamVideoCallParticipant : StreamStatefulModelBase<StreamVideoCallParticipant>, 
        IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>, IStreamVideoCallParticipant
    {
        #region State
        
        public DateTimeOffset JoinedAt {get; private set;}

        public string Role {get; private set;}

        public IStreamVideoUser User { get; set; }

        public string UserSessionId {get; private set;}
        
        #endregion

        public StreamVideoCallParticipant(string uniqueId, ICacheRepository<StreamVideoCallParticipant> repository, IStatefulModelContext context) 
            : base(uniqueId, repository, context)
        {
        }

        protected override string InternalUniqueId { get; set; } //StreamTodo:
        protected override StreamVideoCallParticipant Self => this;

        void IUpdateableFrom<CallParticipantResponseInternalDTO, StreamVideoCallParticipant>.UpdateFromDto(CallParticipantResponseInternalDTO dto, ICache cache)
        {

        }
    }
}