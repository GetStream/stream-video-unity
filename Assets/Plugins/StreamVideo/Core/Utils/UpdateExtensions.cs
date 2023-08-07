using System;
using System.Collections.Generic;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Utils
{
    internal static class UpdateExtensions
    {
        /// <summary>
        /// Clear target list and replace with items created or updated from DTO collection
        /// </summary>
        public static void TryReplaceTrackedObjects<TTracked, TDto>(this IList<TTracked> target, IEnumerable<TDto> dtos,
            ICacheRepository<TTracked> repository)
            where TTracked : class, IStreamStatefulModel, IUpdateableFrom<TDto, TTracked>
        {
            if (target == null)
            {
                throw new ArgumentException(nameof(target));
            }

            if (dtos == null)
            {
                return;
            }

            target.Clear();

            foreach (var dto in dtos)
            {
                var trackedItem = repository.CreateOrUpdate<TTracked, TDto>(dto, out _);
                target.Add(trackedItem);
            }
        }
        
        public static void TryReplaceValuesFromDto(this List<string> target, List<string> values)
            => TryReplaceValuesFromDto<string>(target, values);

        public static void TryReplaceValuesFromDto(this List<int> target, List<int> values)
            => TryReplaceValuesFromDto<int>(target, values);

        private static void TryReplaceValuesFromDto<TValue>(this List<TValue> target, List<TValue> values)
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

            foreach (var dto in values)
            {
                target.Add(dto);
            }
        }
    }
    
    
}