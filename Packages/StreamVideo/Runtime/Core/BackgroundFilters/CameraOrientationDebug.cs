using StreamVideo.Libs.Logs;
using UnityEngine;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Orientation snapshots for background-filter / camera debugging. All output is stripped without STREAM_DEBUG_ENABLED.
    /// Grep logcat / Editor console for <c>BgFilterOrient</c>.
    /// </summary>
    internal static class CameraOrientationDebug
    {
        public const string Prefix = "[BgFilterOrient]";

        public static void Log(ILogs logs, string checkpoint, string payload)
        {
#if STREAM_DEBUG_ENABLED
            if (logs == null || !ShouldEmit(checkpoint, payload))
            {
                return;
            }

            logs.Warning(Prefix + " " + checkpoint + " | " + payload);
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
                + " playing=" + cam.isPlaying
                + " updated=" + cam.didUpdateThisFrame;
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
        private const float HeartbeatSeconds = 5f;

        private static readonly System.Collections.Generic.Dictionary<string, string> LastPayloads
            = new System.Collections.Generic.Dictionary<string, string>();

        private static readonly System.Collections.Generic.Dictionary<string, float> LastEmitTime
            = new System.Collections.Generic.Dictionary<string, float>();

        private static bool ShouldEmit(string checkpoint, string payload)
        {
            if (LastPayloads.TryGetValue(checkpoint, out var previous) && previous == payload)
            {
                if (LastEmitTime.TryGetValue(checkpoint, out var lastTime)
                    && Time.unscaledTime - lastTime < HeartbeatSeconds)
                {
                    return false;
                }
            }

            LastPayloads[checkpoint] = payload;
            LastEmitTime[checkpoint] = Time.unscaledTime;
            return true;
        }
#endif
    }
}
