using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.State.Caches
{
    internal interface ICache
    {
        // ICacheRepository<StreamChannel> Channels { get; }
        // ICacheRepository<StreamMessage> Messages { get; }
        // ICacheRepository<StreamUser> Users { get; }
        // ICacheRepository<StreamLocalUserData> LocalUser { get; }
        // ICacheRepository<StreamChannelMember> ChannelMembers { get; }
        ICacheRepository<StreamCall> Calls { get; }
        ICacheRepository<StreamVideoUser> Users { get; }
    }
}