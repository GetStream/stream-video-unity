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
        public static void TryUpdateOrCreateFromDto<TKey, TDomain, TDto>(this Dictionary<TKey, TDomain> target, IEnumerable<TDto> source, Func<TDto, TKey> keySelector, ICache cache)
            where TDomain : IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (source == null)
            {
                return;
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var dto in source)
            {
                var key = keySelector(dto);
                var item = !target.ContainsKey(key) ? new TDomain() : target[key];
                item.LoadFromDto(dto, cache);
            }
        }
        public static void TryReplaceValuesFromDto<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> dtos)
        {
            if (dtos == null)
            {
                return;
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Clear();

            foreach (var keyValuePair in dtos)
            {
                target.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        
        /// <summary>
        /// If DTOs is null -> do nothing
        /// Otherwise, clear list and populate from DTO collection
        /// </summary>
        public static void TryReplaceEnumsFromDtoCollection<TEnum, TDomain>(this List<TDomain> target, List<TEnum> enums, 
            Func<TEnum, TDomain> converter, ICache cache)
        {
            if (enums == null)
            {
                return;
            }

            target.Clear();

            foreach (var dto in enums)
            {
                var obj = converter(dto);
                target.Add(obj);
            }
        }
        
        /// <summary>
        /// If DTOs is null -> do nothing
        /// Otherwise, clear list and populate from DTO collection
        /// </summary>
        public static void TryReplaceFromDtoCollection<TDto, TDomain>(this List<TDomain> target, List<TDto> dtos, ICache cache)
            where TDomain : IStateLoadableFrom<TDto, TDomain>, new()
        {
            if (dtos == null)
            {
                return;
            }

            target.Clear();

            foreach (var dto in dtos)
            {
                var obj = new TDomain();
                obj.LoadFromDto(dto, cache);
                target.Add(obj);
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