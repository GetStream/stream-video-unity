using StreamVideo.Core.State.Caches;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.State
{
    internal sealed class StatefulModelContext : IStatefulModelContext
    {
        public ICache Cache { get; }
        public IInternalStreamVideoClient Client { get; }
        public ILogs Logs { get; }
        public ISerializer Serializer { get; }

        public StatefulModelContext(ICache cache, IInternalStreamVideoClient client, ISerializer serializer, ILogs logs)
        {
            Cache = cache;
            Client = client;
            Serializer = serializer;
            Logs = logs;
        }
    }
}