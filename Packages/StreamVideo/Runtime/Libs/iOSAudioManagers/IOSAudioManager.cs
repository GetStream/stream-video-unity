using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace StreamVideo.Libs.iOSAudioManagers
{
    /// <summary>
    /// Manages iOS AVAudioSession configuration for WebRTC calls.
    ///
    /// On iOS the SDK uses the same AVAudioSession setup that Zoom / Google Meet /
    /// Microsoft Teams use:
    /// <c>category = PlayAndRecord</c>, <c>mode = VideoChat</c>, options
    /// <c>DefaultToSpeaker | AllowBluetooth | AllowBluetoothA2DP | AllowAirPlay</c>.
    ///
    /// This combination engages the <b>VoiceProcessingIO</b> audio unit, which
    /// gives us hardware echo cancellation, noise suppression and automatic gain
    /// control, and at the same time treats the session as a media-volume session
    /// that defaults to the loudspeaker (not the receiver/earpiece). Wired and
    /// Bluetooth headphones automatically take over when connected.
    ///
    /// The session is configured by the native miniaudio backend at context
    /// creation; <see cref="ConfigureForWebRTC"/> reasserts it after the native
    /// recorder is started in case Unity or another plugin changed it.
    /// </summary>
    public static class IOSAudioManager
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _StreamForceOutputToSpeaker();

        [DllImport("__Internal")]
        private static extern void _StreamClearOutputOverride();

        [DllImport("__Internal")]
        private static extern void _StreamMaximizeInputGain();

        [DllImport("__Internal")]
        private static extern void _StreamConfigureAudioSessionForWebRTC();

        [DllImport("__Internal")]
        private static extern void _StreamDeconfigureAudioSession();

        [DllImport("__Internal")]
        private static extern int _StreamIsHardwareNoiseCancellationActive();

        [DllImport("__Internal")]
        private static extern IntPtr _StreamGetAudioSessionInfo();
#endif

        /// <summary>
        /// Configure AVAudioSession for a WebRTC call. This is the single source
        /// of truth for the iOS audio session - it sets
        /// <c>category = PlayAndRecord</c>, <c>mode = VideoChat</c>, the standard
        /// option set (<c>DefaultToSpeaker | AllowBluetooth | AllowBluetoothA2DP |
        /// AllowAirPlay</c>) and activates the session.
        ///
        /// Must be called BEFORE the native audio recorder is started (the SDK
        /// does this automatically in <c>RtcSession.UpdateAudioRecording</c>) so
        /// that the VoiceProcessingIO audio unit opens with the right mode in
        /// place.
        /// </summary>
        public static void ConfigureForWebRTC()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamConfigureAudioSessionForWebRTC();
#endif
        }

        /// <summary>
        /// Deactivate the AVAudioSession set up by <see cref="ConfigureForWebRTC"/>
        /// and notify other apps so background music / navigation can resume.
        /// Called by the SDK when local audio capture stops.
        /// </summary>
        public static void DeconfigureAudioSession()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamDeconfigureAudioSession();
#endif
        }

        /// <summary>
        /// Hard-override the output to the built-in loudspeaker, even if a wired
        /// or Bluetooth headset is connected. Use sparingly: this overrides the
        /// user's headphone choice. For the standard Meet/Zoom behavior you do
        /// NOT need to call this - <see cref="ConfigureForWebRTC"/> already sets
        /// the loudspeaker as the default route while still letting headphones
        /// take over when connected.
        /// Call <see cref="ClearOutputOverride"/> to undo.
        /// </summary>
        public static void ForceLoudspeaker()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamForceOutputToSpeaker();
#endif
        }

        /// <summary>
        /// Removes a previously applied output override (see
        /// <see cref="ForceLoudspeaker"/>) so the session falls back to the
        /// route iOS would normally pick: headphones if connected, otherwise
        /// the built-in loudspeaker.
        /// </summary>
        public static void ClearOutputOverride()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamClearOutputOverride();
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
        /// Returns true when iOS is currently routing capture/playback through the
        /// VoiceProcessingIO audio unit, which provides hardware-accelerated AEC,
        /// noise suppression and automatic gain control. This is the only noise
        /// cancellation we use on iOS - WebRTC's APM stages are disabled.
        /// </summary>
        /// <remarks>
        /// VoiceProcessingIO is engaged automatically by CoreAudio when
        /// <c>AVAudioSession.category == PlayAndRecord</c> and
        /// <c>AVAudioSession.mode == VoiceChat</c> (or VideoChat).
        /// </remarks>
        public static bool IsHardwareNoiseCancellationActive
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return _StreamIsHardwareNoiseCancellationActive() == 1;
#else
                return false;
#endif
            }
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
