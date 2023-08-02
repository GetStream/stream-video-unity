using StreamVideo.Core.State.Caches;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.State
{
    internal interface IStatefulModelContext
    {
        ICache Cache { get; }
        StreamVideoClient Client { get; }
        ILogs Logs { get; }
        ISerializer Serializer { get; }
    }
}