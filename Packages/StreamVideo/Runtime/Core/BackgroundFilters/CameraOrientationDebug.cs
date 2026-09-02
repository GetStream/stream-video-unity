using StreamVideo.Libs.Logs;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Orientation snapshots for background-filter / camera debugging. All output is stripped without STREAM_DEBUG_ENABLED.
    /// Grep logcat / Editor console for <c>BgFilterOrient</c>.
    /// Identity lines emit once until the payload changes. Mask coverage is batched into periodic summaries.
    /// </summary>
    internal static class CameraOrientationDebug
    {
        public const string Prefix = "[BgFilterOrient]";

        /// <summary>
        /// Log an identity/config snapshot. Repeats of the same checkpoint+payload are suppressed.
        /// </summary>
        public static void Log(ILogs logs, string checkpoint, string payload)
        {
#if STREAM_DEBUG_ENABLED
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
        /// Accumulate per-mask coverage. Logs identity when it changes; otherwise a summary every few seconds.
        /// </summary>
        public static void RecordMask(ILogs logs, string identity, float coverage)
        {
#if STREAM_DEBUG_ENABLED
            if (logs == null)
            {
                return;
            }

            if (identity != _maskIdentity)
            {
                FlushMaskStats(logs);
                _maskIdentity = identity;
                _windowStart = Time.unscaledTime;
                Emit(logs, "mlkit.mask", identity + " coverage=" + coverage.ToString("0.000"));
            }

            _maskCount++;
            _coverageSum += coverage;
            if (coverage < _coverageMin)
            {
                _coverageMin = coverage;
            }

            if (coverage > _coverageMax)
            {
                _coverageMax = coverage;
            }

            if (Time.unscaledTime - _windowStart >= StatsIntervalSeconds)
            {
                FlushMaskStats(logs);
            }
#endif
        }

        public static void Flush(ILogs logs)
        {
#if STREAM_DEBUG_ENABLED
            FlushMaskStats(logs);
            LastPayloads.Clear();
            _maskIdentity = string.Empty;
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

            return "webcam name=\"" + cam.deviceName + "\" front=" + front
                + " requested=" + cam.requestedWidth + "x" + cam.requestedHeight + "@" + cam.requestedFPS
                + " actual=" + cam.width + "x" + cam.height
                + " rot=" + cam.videoRotationAngle
                + " mirrored=" + cam.videoVerticallyMirrored
                + " playing=" + cam.isPlaying;
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

#if STREAM_DEBUG_ENABLED
        private const float StatsIntervalSeconds = 2f;

        private static readonly System.Collections.Generic.Dictionary<string, string> LastPayloads
            = new System.Collections.Generic.Dictionary<string, string>();

        private static string _maskIdentity = string.Empty;
        private static int _maskCount;
        private static float _coverageSum;
        private static float _coverageMin = 1f;
        private static float _coverageMax;
        private static float _windowStart;

        private static void FlushMaskStats(ILogs logs)
        {
            if (_maskCount <= 0)
            {
                ResetMaskWindow();
                return;
            }

            var avg = _coverageSum / _maskCount;
            var dt = Mathf.Max(0f, Time.unscaledTime - _windowStart);
            Emit(logs, "mlkit.mask.stats",
                "n=" + _maskCount
                + " dt=" + dt.ToString("0.0") + "s"
                + " coverage=" + avg.ToString("0.000")
                + " [" + _coverageMin.ToString("0.000") + "-" + _coverageMax.ToString("0.000") + "]"
                + " | " + _maskIdentity);
            ResetMaskWindow();
        }

        private static void ResetMaskWindow()
        {
            _maskCount = 0;
            _coverageSum = 0f;
            _coverageMin = 1f;
            _coverageMax = 0f;
            _windowStart = Time.unscaledTime;
        }

        private static void Emit(ILogs logs, string checkpoint, string payload)
        {
            var line = (logs.Prefix ?? string.Empty) + Prefix + " " + checkpoint + " | " + payload;
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0}", line);
        }
#endif
    }
}
