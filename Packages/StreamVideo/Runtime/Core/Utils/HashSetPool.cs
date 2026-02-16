using System;
using System.Collections.Generic;

namespace StreamVideo.Core.Utils
{
    internal static class HashSetPool<T>
    {
        public static HashSet<T> Rent()
        {
            if (Pool.Count > 0)
            {
                var set = Pool.Pop();
                return set;
            }

            return new HashSet<T>();
        }

        public static void Release(HashSet<T> set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            set.Clear();

            if (Pool.Count < MaxPoolSize)
            {
                Pool.Push(set);
            }
        }

        private const int MaxPoolSize = 128;
        private static readonly Stack<HashSet<T>> Pool = new Stack<HashSet<T>>();
    }
}
