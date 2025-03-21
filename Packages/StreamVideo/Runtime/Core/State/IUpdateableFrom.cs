using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.State
{
    //StreamTODO: rename to IStateUpdateableFrom or IStreamStateUpdateableFrom? 
    /// <summary>
    /// Allows to update <see cref="IStreamStatefulModel"/> object from DTO
    /// </summary>
    /// <typeparam name="TDto">DTO received from a server response</typeparam>
    /// <typeparam name="TTrackedObject">Object with a tracked state. This object resides in the internal cache and is bound to its server counter-part object by a unique ID</typeparam>
    internal interface IUpdateableFrom<in TDto, out TTrackedObject>
        where TTrackedObject : IStreamStatefulModel, IUpdateableFrom<TDto, TTrackedObject>
    {
        void UpdateFromDto(TDto dto, ICache cache);
    }
}