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
    ///
    /// <para>
    /// All non-property members of this class are <b>iOS-only</b>. On any other
    /// build target (Editor, Standalone, Android, WebGL, ...) they throw
    /// <see cref="PlatformNotSupportedException"/>. This is deliberate: silently
    /// no-oping would let calling code believe the configuration succeeded
    /// when nothing actually happened. Use <see cref="IsSupported"/> if you
    /// need a runtime gate without <c>#if</c> directives.
    /// </para>
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
        private static extern int _StreamConfigureAudioSessionForWebRTC();

        [DllImport("__Internal")]
        private static extern void _StreamDeconfigureAudioSession();

        [DllImport("__Internal")]
        private static extern int _StreamIsHardwareNoiseCancellationActive();

        [DllImport("__Internal")]
        private static extern IntPtr _StreamGetAudioSessionInfo();
#endif

        /// <summary>
        /// <c>true</c> when the current build target is iOS device (i.e. the
        /// underlying AVAudioSession native plugin is reachable). On every
        /// other target this returns <c>false</c> and every other member of
        /// this class will throw <see cref="PlatformNotSupportedException"/>.
        ///
        /// Use this to gate calls without <c>#if UNITY_IOS</c> directives, e.g.
        /// <code>
        /// if (IOSAudioManager.IsSupported)
        /// {
        ///     IOSAudioManager.ConfigureForWebRTC();
        /// }
        /// </code>
        /// </summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Configure AVAudioSession for a WebRTC call. This is the single source
        /// of truth for the iOS audio session - it sets
        /// <c>category = PlayAndRecord</c>, <c>mode = VideoChat</c>, the standard
        /// option set (<c>DefaultToSpeaker | AllowBluetooth | AllowBluetoothA2DP |
        /// AllowAirPlay</c>) and activates the session. It also installs (once)
        /// AVAudioSession interruption and media-services-reset observers
        /// that re-apply the configuration if the system tears it down while
        /// the SDK still wants it up.
        ///
        /// Must be called BEFORE the native audio engine opens the
        /// VoiceProcessingIO audio unit. On iOS the same MiniaudioDuplexDevice
        /// services both capture and playback through one VPIO unit, so the
        /// session has to be in PlayAndRecord BEFORE either
        /// <c>WebRTC.StartAudioPlayback</c> (called from <c>RtcSession.DoJoin</c>)
        /// or <c>StartLocalAudioCapture</c> (called from
        /// <c>RtcSession.UpdateAudioRecording</c>). The SDK does this
        /// automatically in both call sites; the call is idempotent so
        /// re-asserting it does no harm.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the session ended up in
        /// <c>PlayAndRecord</c> + <c>VideoChat</c>/<c>VoiceChat</c> and was
        /// successfully activated; <c>false</c> if any step failed (details
        /// are written to the iOS device log under
        /// <c>[StreamVideo iOS Audio]</c>).
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static bool ConfigureForWebRTC()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _StreamConfigureAudioSessionForWebRTC() == 1;
#else
            throw NotSupported(nameof(ConfigureForWebRTC));
#endif
        }

        /// <summary>
        /// Deactivate the AVAudioSession set up by <see cref="ConfigureForWebRTC"/>
        /// and notify other apps so background music / navigation can resume.
        /// Called by the SDK when local audio capture stops.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static void DeconfigureAudioSession()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamDeconfigureAudioSession();
#else
            throw NotSupported(nameof(DeconfigureAudioSession));
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
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static void ForceLoudspeaker()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamForceOutputToSpeaker();
#else
            throw NotSupported(nameof(ForceLoudspeaker));
#endif
        }

        /// <summary>
        /// Removes a previously applied output override (see
        /// <see cref="ForceLoudspeaker"/>) so the session falls back to the
        /// route iOS would normally pick: headphones if connected, otherwise
        /// the built-in loudspeaker.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static void ClearOutputOverride()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamClearOutputOverride();
#else
            throw NotSupported(nameof(ClearOutputOverride));
#endif
        }

        /// <summary>
        /// Maximize microphone input gain.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static void MaximizeInputGain()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamMaximizeInputGain();
#else
            throw NotSupported(nameof(MaximizeInputGain));
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
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
        public static bool IsHardwareNoiseCancellationActive
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return _StreamIsHardwareNoiseCancellationActive() == 1;
#else
                throw NotSupported(nameof(IsHardwareNoiseCancellationActive));
#endif
            }
        }

        /// <summary>
        /// Returns a human-readable string describing the current iOS audio session state.
        /// Useful for diagnostics.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown on any non-iOS build target.
        /// </exception>
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
            throw NotSupported(nameof(GetCurrentSettings));
#endif
        }

#if !(UNITY_IOS && !UNITY_EDITOR)
        private static PlatformNotSupportedException NotSupported(string member)
            => new PlatformNotSupportedException(
                $"{nameof(IOSAudioManager)}.{member} is only available on iOS device builds. "
                + $"Gate the call with `if ({nameof(IOSAudioManager)}.{nameof(IsSupported)})` "
                + "or `#if UNITY_IOS && !UNITY_EDITOR`.");
#endif
    }
}
