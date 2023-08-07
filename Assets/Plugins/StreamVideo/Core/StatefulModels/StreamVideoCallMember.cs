using System;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.StatefulModels
{
    //StreamTodo: call member could be a StreamStatefulModelBase obj because it seems to be unique per (call id + user id)
    //but because the model itself doesn't have the call ID we never know to which call it belongs from the model scope
    // Alternative would be to modify RegisterDtoIdMapping and not always relay on DTO to have a unique ID but allow to inject it
    [Obsolete("Use CallMember until we figure out if this could be a stateful model")]
    internal class StreamVideoCallMember : StreamStatefulModelBase<StreamVideoCallMember>, IStreamVideoCallMember
    {
        internal StreamVideoCallMember(string uniqueId, ICacheRepository<StreamVideoCallMember> repository, IStatefulModelContext context) 
            : base(uniqueId, repository, context)
        {
        }

        protected override string InternalUniqueId { get; set; }
        protected override StreamVideoCallMember Self => this;
    }
}