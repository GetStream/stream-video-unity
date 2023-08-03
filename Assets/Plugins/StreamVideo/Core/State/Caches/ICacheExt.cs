using StreamVideo.Core.InternalDTO.Responses;

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
    }
}