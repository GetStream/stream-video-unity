using System;
using System.Collections.Generic;
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
        void LoadFromDto(TDto dto, ICache cache);
    }
    
    /// <summary>
    /// Extensions for <see cref="IStateLoadableFrom{TDto, TDomain}"/>
    /// </summary>
    internal static class StateLoadableFromExt
    {
        public static void TryReplaceValuesFromDto<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> values)
        {
            if (values == null)
            {
                return;
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Clear();

            foreach (var keyValuePair in values)
            {
                target.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        
        public static void TryLoadFromDtoCollection<TDto, TDomain>(this ICache cache, List<TDomain> target, List<TDto> dtos)
            where TDomain : IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (dtos == null)
            {
                return;
            }

            var items = new List<TDomain>(dtos.Count);

            foreach (var dto in dtos)
            {
                var obj = new TDomain();
                obj.LoadFromDto(dto, cache);
                items.Add(obj);
            }
        }
        
        public static void ReplaceFromDtoCollection<TDto, TDomain>(this List<TDomain> target, List<TDto> dtos, ICache cache)
            where TDomain : IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (dtos == null)
            {
                return;
            }

            var items = new List<TDomain>(dtos.Count);

            foreach (var dto in dtos)
            {
                var obj = new TDomain();
                obj.LoadFromDto(dto, cache);
                items.Add(obj);
            }
        }
        
        /// <summary>
        /// If DTO is null -> return null
        /// Otherwise update state from DTO
        /// Create new object if <see cref="target"/> was null
        /// </summary>
        /// <example>
        /// Egress = cache.CreateFromDtoOrNull(Egress, dto.Egress);
        /// </example>
        public static TDomain TryUpdateOrCreateFromDto<TDto, TDomain>(this ICache cache, TDomain target, TDto dto)
            where TDomain : class, IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (dto == null)
            {
                return null;
            }

            if (target == null)
            {
                target = new TDomain();
            }

            target.LoadFromDto(dto, cache);
            return target;
        }
    }
}