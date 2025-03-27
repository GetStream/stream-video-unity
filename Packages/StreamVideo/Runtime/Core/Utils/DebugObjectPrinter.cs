using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamVideo.Core.Utils
{
#if STREAM_DEBUG_ENABLED
    internal class DebugObjectPrinter
    {
        public static string PrintObject(object obj, int maxDepth = 10)
            => PrintObjectInternal(obj, new HashSet<object>(), 0, maxDepth);

        private static string PrintObjectInternal(object obj, HashSet<object> visited, int depth, int maxDepth)
        {
            if (obj == null)
                return "null";

            if (depth >= maxDepth)
                return $"[Max depth {maxDepth} reached]";

            // Handle primitive types and strings
            if (obj.GetType().IsPrimitive || obj is string || obj is DateTime)
                return obj.ToString();

            // Prevent circular references
            if (!obj.GetType().IsValueType)
            {
                if (visited.Contains(obj))
                    return "[Circular Reference]";
                visited.Add(obj);
            }

            var sb = new StringBuilder();
            var type = obj.GetType();

            // Handle collections
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                sb.Append($"{type.Name} [");
                bool first = true;
                foreach (var item in enumerable)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(PrintObjectInternal(item, visited, depth + 1, maxDepth));
                    first = false;
                }

                sb.Append("]");
                return sb.ToString();
            }

            // Handle objects
            sb.AppendLine($"{type.Name} {{");

            // Get all fields (both public and private)
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                        BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                sb.AppendLine($"  {field.Name} ({field.Attributes}): " +
                              PrintObjectInternal(value, visited, depth + 1, maxDepth));
            }

            // Get all properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Instance | BindingFlags.Static);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj, null);
                    sb.AppendLine($"  {prop.Name} ({prop.Attributes}): " +
                                  PrintObjectInternal(value, visited, depth + 1, maxDepth));
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  {prop.Name}: [Error accessing property: {ex.Message}]");
                }
            }

            sb.Append("}");
            return sb.ToString();
        }
    }
#endif
}