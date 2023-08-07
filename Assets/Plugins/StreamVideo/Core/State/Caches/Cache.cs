using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.State.Caches
{
    internal sealed class Cache : ICache
    {
        public Cache(StreamVideoClient stateClient, ISerializer serializer, ILogs logs)
        {
            var trackedObjectsFactory = new StatefulModelsFactory(stateClient, serializer, logs, this);
            
            Calls = new CacheRepository<StreamCall>(trackedObjectsFactory.CreateStreamCall, cache: this);
            Users = new CacheRepository<StreamVideoUser>(trackedObjectsFactory.CreateStreamVideoUser, cache: this);
            CallParticipants = new CacheRepository<StreamVideoCallParticipant>(trackedObjectsFactory.CreateStreamVideoCallParticipant, cache: this);
            
            //StreamTodo: validate that all mappings are registered:
            //grab IUpdateableFrom interface from each model and check if every DTO is registered
            Calls.RegisterDtoIdMapping<StreamCall, CallResponseInternalDTO>(dto => dto.Cid);
            Calls.RegisterDtoIdMapping<StreamCall, GetCallResponseInternalDTO>(dto => dto.Call.Cid);
            Calls.RegisterDtoIdMapping<StreamCall, GetOrCreateCallResponseInternalDTO>(dto => dto.Call.Cid);
            Calls.RegisterDtoIdMapping<StreamCall, JoinCallResponseInternalDTO>(dto => dto.Call.Cid);
            
            Users.RegisterDtoIdMapping<StreamVideoUser, UserResponseInternalDTO>(dto => dto.Id);
            
            CallParticipants.RegisterDtoIdMapping<StreamVideoCallParticipant, CallParticipantResponseInternalDTO>(dto => dto.UserSessionId);

            // Channels = new CacheRepository<StreamChannel>(trackedObjectsFactory.CreateStreamChannel, cache: this);
            // Messages = new CacheRepository<StreamMessage>(trackedObjectsFactory.CreateStreamMessage, cache: this);
            // Users = new CacheRepository<StreamUser>(trackedObjectsFactory.CreateStreamUser, cache: this);
            // LocalUser = new CacheRepository<StreamLocalUserData>(trackedObjectsFactory.CreateStreamLocalUser, cache: this);
            // ChannelMembers = new CacheRepository<StreamChannelMember>(trackedObjectsFactory.CreateStreamChannelMember, cache: this);
            //
            // Channels.RegisterDtoIdMapping<StreamChannel, ChannelStateResponseInternalDTO>(dto => dto.Channel.Cid);
            // Channels.RegisterDtoIdMapping<StreamChannel, ChannelResponseInternalDTO>(dto => dto.Cid);
            // Channels.RegisterDtoIdMapping<StreamChannel, ChannelStateResponseFieldsInternalDTO>(dto => dto.Channel.Cid);
            // Channels.RegisterDtoIdMapping<StreamChannel, UpdateChannelResponseInternalDTO>(dto => dto.Channel.Cid);
            //
            // Users.RegisterDtoIdMapping<StreamUser, UserObjectInternalDTO>(dto => dto.Id);
            // Users.RegisterDtoIdMapping<StreamUser, UserResponseInternalDTO>(dto => dto.Id);
            // Users.RegisterDtoIdMapping<StreamUser, OwnUserInternalDTO>(dto => dto.Id);
            //
            // LocalUser.RegisterDtoIdMapping<StreamLocalUserData, OwnUserInternalDTO>(dto => dto.Id);
            //
            // //In some cases the ChannelMemberInternalDTO.UserId was null
            // ChannelMembers.RegisterDtoIdMapping<StreamChannelMember, ChannelMemberInternalDTO>(dto => dto.User.Id);
            //
            // Messages.RegisterDtoIdMapping<StreamMessage, MessageInternalDTO>(dto => dto.Id);
        }

         public ICacheRepository<StreamCall> Calls { get; }
         public ICacheRepository<StreamVideoUser> Users { get; }
         public ICacheRepository<StreamVideoCallParticipant> CallParticipants { get; }

         // public ICacheRepository<StreamChannel> Channels { get; }
        //
        // public ICacheRepository<StreamMessage> Messages { get; }
        //
        // public ICacheRepository<StreamUser> Users { get; }
        //
        // public ICacheRepository<StreamLocalUserData> LocalUser { get; }
        //
        // public ICacheRepository<StreamChannelMember> ChannelMembers { get; }
    }
}