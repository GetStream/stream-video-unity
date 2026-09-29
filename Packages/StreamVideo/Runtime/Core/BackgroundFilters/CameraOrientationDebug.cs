using StreamVideo.Libs.Logs;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Orientation snapshots for background-filter / camera debugging.
    /// Output is compiled out without STREAM_DEBUG_ENABLED, and is a no-op without STREAM_LOG_BG_FILTER.
    /// Grep logcat / Editor console for <c>BgFilterOrient</c>.
    /// Identity lines emit once until the payload changes. Mask coverage logs on identity change or a large jump.
    /// </summary>
    internal static class CameraOrientationDebug
    {
        public const string Prefix = "[BgFilterOrient]";

        // 16 is Unity's observed dummy width until a capture buffer arrives. Not documented.
        internal static bool CanReadWebCamOrientation(WebCamTexture cam)
            => cam != null && cam.isPlaying && cam.width > 16;

        internal const float MaskCoverageJumpThreshold = 0.15f;

        internal static bool ShouldLogMaskCoverage(float previousCoverage, float currentCoverage)
            => Mathf.Abs(currentCoverage - previousCoverage) >= MaskCoverageJumpThreshold;

        /// <summary>
        /// Log an identity/config snapshot. Repeats of the same checkpoint+payload are suppressed.
        /// </summary>
        public static void Log(ILogs logs, string checkpoint, string payload)
        {
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            if (logs == null || string.IsNullOrEmpty(checkpoint))
            {
                return;
            }

            if (LastPayloads.TryGetValue(checkpoint, out var previous) && previous == payload)
            {
                return;
            }

            LastPayloads[checkpoint] = payload;
            Emit(logs, checkpoint, payload);
#endif
        }

        /// <summary>
        /// Log mask coverage when the identity string changes or coverage jumps by
        /// <see cref="MaskCoverageJumpThreshold"/>. No periodic timer.
        /// </summary>
        public static void RecordMask(ILogs logs, string identity, float coverage)
        {
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            if (logs == null)
            {
                return;
            }

            if (identity != _maskIdentity)
            {
                _maskIdentity = identity;
                _lastLoggedCoverage = coverage;
                Emit(logs, "mlkit.mask", identity + " coverage=" + coverage.ToString("0.000"));
                return;
            }

            if (!ShouldLogMaskCoverage(_lastLoggedCoverage, coverage))
            {
                return;
            }

            _lastLoggedCoverage = coverage;
            Emit(logs, "mlkit.mask", identity + " coverage=" + coverage.ToString("0.000"));
#endif
        }

        public static void Flush(ILogs logs)
        {
#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
            LastPayloads.Clear();
            _maskIdentity = string.Empty;
            _lastLoggedCoverage = 0f;
#endif
        }

        public static string DescribeScreen()
        {
#if STREAM_DEBUG_ENABLED
            return "screen=" + Screen.width + "x" + Screen.height
                + " orientation=" + Screen.orientation
                + " gfx=" + SystemInfo.graphicsDeviceType;
#else
            return string.Empty;
#endif
        }

        public static string DescribeWebCam(WebCamTexture cam)
        {
#if STREAM_DEBUG_ENABLED
            if (cam == null)
            {
                return "webcam=null";
            }

            var front = false;
            var devices = WebCamTexture.devices;
            for (var i = 0; i < devices.Length; i++)
            {
                if (devices[i].name == cam.deviceName)
                {
                    front = devices[i].isFrontFacing;
                    break;
                }
            }

            var line = "webcam name=\"" + cam.deviceName + "\" front=" + front
                + " requested=" + cam.requestedWidth + "x" + cam.requestedHeight + "@" + cam.requestedFPS
                + " actual=" + cam.width + "x" + cam.height
                + " playing=" + cam.isPlaying;

            if (CanReadWebCamOrientation(cam))
            {
                line += " rot=" + cam.videoRotationAngle
                    + " mirrored=" + cam.videoVerticallyMirrored;
            }
            else
            {
                line += " rot=pending mirrored=pending";
            }

            return line;
#else
            return string.Empty;
#endif
        }

        public static string DescribeTexture(string label, Texture texture)
        {
#if STREAM_DEBUG_ENABLED
            if (texture == null)
            {
                return label + "=null";
            }

            return label + "=" + texture.width + "x" + texture.height + " name=" + texture.name
                + " type=" + texture.GetType().Name;
#else
            return string.Empty;
#endif
        }

#if STREAM_DEBUG_ENABLED && STREAM_LOG_BG_FILTER
        private static readonly System.Collections.Generic.Dictionary<string, string> LastPayloads
            = new System.Collections.Generic.Dictionary<string, string>();

        private static string _maskIdentity = string.Empty;
        private static float _lastLoggedCoverage;

        private static void Emit(ILogs logs, string checkpoint, string payload)
        {
            var line = (logs.Prefix ?? string.Empty) + Prefix + " " + checkpoint + " | " + payload;
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", line);
        }
#endif
    }
}
