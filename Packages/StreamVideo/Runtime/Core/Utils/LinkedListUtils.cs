using System.Collections.Generic;

namespace StreamVideo.Core.Utils
{
    internal static class LinkedListUtils
    {
        public static void RemoveAll<T>(this LinkedList<T> list, T item)
        {
            for (var node = list.First; node != null;)
            {
                var nextNode = node.Next;
                if (node.Value.Equals(item))
                {
                    list.Remove(node);
                }

                node = nextNode;
            }
        }

        public static int IndexOf<T>(this LinkedList<T> list, T item)
        {
            var index = 0;
            for (var node = list.First; node != null; node = node.Next, index++)
            {
                if (item.Equals(node.Value))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}