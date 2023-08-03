using System;
using System.Collections.Generic;

namespace StreamVideo.Core.Utils
{
    internal static class UpdateExtensions
    {
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