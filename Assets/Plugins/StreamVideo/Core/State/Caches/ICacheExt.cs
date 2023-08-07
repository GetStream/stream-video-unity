using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.StatefulModels;

namespace StreamVideo.Core.State.Caches
{
    internal static class ICacheExt
    {
        public static StreamCall TryCreateOrUpdate(this ICache cache, CallResponseInternalDTO dto)
            => dto == null ? null : cache.Calls.CreateOrUpdate<StreamCall, CallResponseInternalDTO>(dto, out _);
        
        public static StreamCall TryCreateOrUpdate(this ICache cache, GetCallResponseInternalDTO dto)
            => dto == null ? null : cache.Calls.CreateOrUpdate<StreamCall, GetCallResponseInternalDTO>(dto, out _);
        
        public static StreamCall TryCreateOrUpdate(this ICache cache, GetOrCreateCallResponseInternalDTO dto)
            => dto == null ? null : cache.Calls.CreateOrUpdate<StreamCall, GetOrCreateCallResponseInternalDTO>(dto, out _);
        
        public static StreamVideoUser TryCreateOrUpdate(this ICache cache, UserResponseInternalDTO dto)
            => dto == null ? null : cache.Users.CreateOrUpdate<StreamVideoUser, UserResponseInternalDTO>(dto, out _);
    }
}