using System;
using System.Collections.Generic;

namespace StreamVideo.Core.Utils
{
    internal static class ListPool<T>
    {
        public static List<T> Rent()
        {
            if (Pool.Count > 0)
            {
                var list = Pool.Pop();
                return list;
            }

            return new List<T>();
        }

        public static void Release(List<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            list.Clear();

            if (Pool.Count < MaxPoolSize)
            {
                Pool.Push(list);
            }
        }

        private const int MaxPoolSize = 128;
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>();
    }
}