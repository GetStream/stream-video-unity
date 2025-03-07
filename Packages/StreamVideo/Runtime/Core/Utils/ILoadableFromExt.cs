namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// Extensions for <see cref="ILoadableFrom{TDto,TDomain}"/>
    /// </summary>
    internal static class ILoadableFromExt
    {
        /// <summary>
        /// Load domain object from the DTO. If the loadable is null, creates a new instance of the domain object.
        /// </summary>
        public static TDomain TryCreateOrLoadFromDto<TDto, TDomain>(this ILoadableFrom<TDto, TDomain> loadable, TDto dto)
            where TDomain : ILoadableFrom<TDto, TDomain>, new()
        {
            if (dto == null)
            {
                return default;
            }

            return loadable != null ? loadable.LoadFromDto(dto) : new TDomain().LoadFromDto(dto);
        }
    }
}