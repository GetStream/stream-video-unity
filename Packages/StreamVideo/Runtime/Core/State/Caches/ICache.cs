using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.State.Caches
{
    internal interface ICache
    {
        ICacheRepository<StreamCall> Calls { get; }
        ICacheRepository<StreamVideoUser> Users { get; }
        ICacheRepository<StreamVideoCallParticipant> CallParticipants { get; }
    }
}