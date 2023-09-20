using System;
using System.Collections.Generic;
using StreamVideo.Core.State;
using StreamVideo.Core.State.Caches;

namespace StreamVideo.Core.Utils
{
    internal static class UpdateExtensions<TTracked, TDto>
        where TTracked : class, IStreamStatefulModel, IUpdateableFrom<TDto, TTracked>
    {
        public static void TryAddUniqueTrackedObjects(IList<TTracked> target, IEnumerable<TDto> dtos,
            ICacheRepository<TTracked> repository)

        {
            if (target == null)
            {
                throw new ArgumentException(nameof(target));
            }

            if (dtos == null)
            {
                return;
            }
            
            UniqueItems.Clear();
            foreach (var item in target)
            {
                UniqueItems.Add(item);
            }

            foreach (var dto in dtos)
            {
                var trackedItem = repository.CreateOrUpdate<TTracked, TDto>(dto, out _);

                if (!UniqueItems.Contains(trackedItem))
                {
                    target.Add(trackedItem);
                }
            }
            
            UniqueItems.Clear();
        }

        private static readonly HashSet<TTracked> UniqueItems = new HashSet<TTracked>();
    }
   
    internal static class UpdateExtensions
    {
        //StreamTodo: we could turn this into smart sync:
        //1. remove from target what is not present in dtos
        //2. add to target what is present in dtos but missing in target
        //3. update the overlapping items
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
        

        
        public static void TryReplaceValuesFromDto(this List<string> target, IEnumerable<string> values)
            => TryReplaceValuesFromDto<string>(target, values);

        public static void TryReplaceValuesFromDto(this List<int> target, IEnumerable<int> values)
            => TryReplaceValuesFromDto<int>(target, values);

        private static void TryReplaceValuesFromDto<TValue>(this List<TValue> target, IEnumerable<TValue> values)
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