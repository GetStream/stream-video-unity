using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.State
{
    internal interface IUpdateableFrom<in TDto, out TTrackedObject>
        where TTrackedObject : IStreamStatefulModel, IUpdateableFrom<TDto, TTrackedObject>
    {
        void UpdateFromDto(TDto dto, ICache cache);
    }
}