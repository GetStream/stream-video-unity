using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    /// <summary>
    /// Device trace for keyboard avoidance geometry. Calls to <see cref="Log"/>, including their arguments,
    /// are compiled out without STREAM_DEBUG_ENABLED. Grep logcat for <c>[KbAvoid]</c>.
    /// A checkpoint logs only when its payload changes, so per-frame polling does not flood the log.
    /// </summary>
    internal static class KeyboardAvoidanceDebug
    {
        public const string Prefix = "[KbAvoid]";

        [System.Diagnostics.Conditional("STREAM_DEBUG_ENABLED")]
        public static void Log(string checkpoint, string payload)
        {
            lock (LastPayloads)
            {
                if (LastPayloads.TryGetValue(checkpoint, out var previous) && previous == payload)
                {
                    return;
                }

                LastPayloads[checkpoint] = payload;
            }

            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0} {1} {2}", Prefix, checkpoint, payload);
        }

        public static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public static string Describe(Rect rect)
            => "(x=" + F(rect.x) + " y=" + F(rect.y) + " w=" + F(rect.width) + " h=" + F(rect.height) + ")";

        public static string Describe(NormalizedRect rect)
            => "(x=" + F(rect.X) + " y=" + F(rect.Y) + " w=" + F(rect.Width) + " h=" + F(rect.Height) + ")";

        public static string Describe(PanelRect rect)
            => "[" + F(rect.XMin) + "," + F(rect.YMin) + " .. " + F(rect.XMax) + "," + F(rect.YMax) + "]";

        public static string Describe(KeyboardGeometrySnapshot snapshot)
        {
            var builder = new StringBuilder();
            builder.Append("state=").Append(snapshot.State)
                .Append(" valid=").Append(snapshot.IsValid)
                .Append(" provider=").Append(snapshot.Provider)
                .Append(" occlusions=").Append(snapshot.OcclusionCount);
            for (var i = 0; i < snapshot.OcclusionCount; i++)
            {
                builder.Append(' ').Append(Describe(snapshot.Occlusion(i)));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats <paramref name="count"/> left, top, right, bottom pixel rectangles packed into <paramref name="bounds"/>.
        /// </summary>
        public static string DescribePixels(int[] bounds, int count)
        {
            var builder = new StringBuilder();
            builder.Append(count);
            for (var i = 0; i < count; i++)
            {
                var offset = i * 4;
                builder.Append(" [").Append(bounds[offset]).Append(',').Append(bounds[offset + 1])
                    .Append(" .. ").Append(bounds[offset + 2]).Append(',').Append(bounds[offset + 3]).Append(']');
            }

            return builder.ToString();
        }

        private static readonly Dictionary<string, string> LastPayloads = new Dictionary<string, string>();
    }
}
