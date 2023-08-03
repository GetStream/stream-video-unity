using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.StatefulModels
{
    internal class StreamCallMember : StreamStatefulModelBase<StreamCallMember>, IStreamVideoUser
    {
        internal StreamCallMember(string uniqueId, ICacheRepository<StreamCallMember> repository, IStatefulModelContext context) 
            : base(uniqueId, repository, context)
        {
        }

        protected override string InternalUniqueId { get; set; }
        protected override StreamCallMember Self => this;
    }
}