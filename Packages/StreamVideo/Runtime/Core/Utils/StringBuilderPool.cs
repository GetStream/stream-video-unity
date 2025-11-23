using System;
using System.Collections.Generic;
using System.Text;

namespace StreamVideo.Core.Utils
{
    internal static class StringBuilderPool
    {
        public static StringBuilder Rent()
        {
            if (Pool.Count > 0)
            {
                var sb = Pool.Pop();
                sb.Clear();
                return sb;
            }

            return new StringBuilder();
        }

        public static void Release(StringBuilder sb)
        {
            if (sb == null)
            {
                throw new ArgumentNullException(nameof(sb));
            }

            sb.Clear();

            if (Pool.Count < MaxPoolSize)
            {
                Pool.Push(sb);
            }
        }

        private const int MaxPoolSize = 128;
        private static readonly Stack<StringBuilder> Pool = new Stack<StringBuilder>();
    }
}