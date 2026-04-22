using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace StreamVideo.Libs.iOSAudioManagers
{
    /// <summary>
    /// Manages iOS AVAudioSession configuration for WebRTC calls.
    /// Speaker routing and input gain are handled here; audio session
    /// category/mode are managed by the native miniaudio integration.
    /// </summary>
    public static class IOSAudioManager
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _StreamForceOutputToSpeaker();

        [DllImport("__Internal")]
        private static extern void _StreamMaximizeInputGain();

        [DllImport("__Internal")]
        private static extern void _StreamConfigureAudioSessionForWebRTC();

        [DllImport("__Internal")]
        private static extern IntPtr _StreamGetAudioSessionInfo();
#endif

        /// <summary>
        /// Configure the audio session for WebRTC after the native recorder has started.
        /// This applies speaker routing and input gain optimizations without overriding
        /// the category/mode that miniaudio has already set.
        /// </summary>
        public static void ConfigureForWebRTC()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamConfigureAudioSessionForWebRTC();
#endif
        }

        /// <summary>
        /// Force audio output to the loudspeaker instead of the earpiece.
        /// </summary>
        public static void ForceLoudspeaker()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamForceOutputToSpeaker();
#endif
        }

        /// <summary>
        /// Maximize microphone input gain.
        /// </summary>
        public static void MaximizeInputGain()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamMaximizeInputGain();
#endif
        }

        /// <summary>
        /// Returns a human-readable string describing the current iOS audio session state.
        /// Useful for diagnostics.
        /// </summary>
        public static string GetCurrentSettings()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                IntPtr ptr = _StreamGetAudioSessionInfo();
                if (ptr == IntPtr.Zero)
                    return "Failed to get audio session info";

                string info = Marshal.PtrToStringAnsi(ptr);
                Marshal.FreeHGlobal(ptr);
                return info;
            }
            catch (Exception e)
            {
                return $"Error getting audio info: {e.Message}";
            }
#else
            return "iOS audio session info is only available on iOS devices.";
#endif
        }
    }
}
