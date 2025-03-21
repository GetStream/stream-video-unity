using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Extensions for <see cref="ISavableTo{TDto}"/>
    /// </summary>
    internal static class ISavableToExt
    {
        public static TDto TrySaveToDto<TDto>(this ISavableTo<TDto> source)
            => source != default ? source.SaveToDto() : default;
    }
}