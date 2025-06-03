using StreamVideo.Core.InternalDTO.Models;
using StreamVideo.v1.Sfu.Models;
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
            Calls.RegisterDtoIdMapping<StreamCall, CallStateResponseFieldsInternalDTO>(dto => dto.Call.Cid);
            
            Users.RegisterDtoIdMapping<StreamVideoUser, UserResponseInternalDTO>(dto => dto.Id);
            
            CallParticipants.RegisterDtoIdMapping<StreamVideoCallParticipant, CallParticipantResponseInternalDTO>(dto => dto.UserSessionId);
            CallParticipants.RegisterDtoIdMapping<StreamVideoCallParticipant, Participant>(dto => dto.SessionId);
        }

         public ICacheRepository<StreamCall> Calls { get; }
         public ICacheRepository<StreamVideoUser> Users { get; }
         public ICacheRepository<StreamVideoCallParticipant> CallParticipants { get; }
    }
}