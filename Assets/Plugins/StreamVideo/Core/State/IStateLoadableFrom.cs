using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.State
{
    /// <summary>
    /// Supports loading object from DTO of a given type.
    /// This is different from <see cref="ILoadableFrom{TDto,TDomain}"/> because it loads data from the app state if needed
    /// </summary>
    /// <typeparam name="TDto">DTO type</typeparam>
    /// <typeparam name="TDomain">Domain object type</typeparam>
    internal interface IStateLoadableFrom<in TDto, out TDomain>
        where TDomain : IStateLoadableFrom<TDto, TDomain>
    {
        TDomain LoadFromDto(TDto dto, ICache cache);
    }
    
    /// <summary>
    /// Extensions for <see cref="IStateLoadableFrom{TDto, TDomain}"/>
    /// </summary>
    internal static class StateLoadableFromExt
    {
        public static TDomain TryLoadFromDto<TDto, TDomain>(this IStateLoadableFrom<TDto, TDomain> loadable, TDto dto, ICache cache)
            where TDomain : class, IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (dto == null)
            {
                return null;
            }

            return new TDomain().LoadFromDto(dto, cache);
        }
    }
}