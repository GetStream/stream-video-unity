using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.StatefulModels
{
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