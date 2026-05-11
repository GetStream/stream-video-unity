using System;
using System.Runtime.InteropServices;

namespace StreamVideo.Libs.iOSAudioManagers
{
    /// <summary>
    /// Manages iOS AVAudioSession configuration for WebRTC calls.
    /// Sets <c>category = PlayAndRecord</c> and <c>mode = VideoChat</c> with options
    /// <c>DefaultToSpeaker | AllowBluetooth | AllowBluetoothA2DP | AllowAirPlay</c>,
    /// which engages the <b>VoiceProcessingIO</b> audio unit (hardware AEC, NS, AGC)
    /// and the same media-volume / loudspeaker-default routing used by Zoom, Meet
    /// and Teams.
    ///
    /// <para>
    /// All members except <see cref="IsSupported"/> are <b>iOS-only</b> and throw
    /// <see cref="PlatformNotSupportedException"/> on every other build target.
    /// Use <see cref="IsSupported"/> to gate calls without <c>#if</c> directives.
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
        /// <c>true</c> on iOS device builds (the underlying native plugin is reachable);
        /// <c>false</c> elsewhere. When <c>false</c>, every other member throws.
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
        /// Configure AVAudioSession for a WebRTC call (PlayAndRecord + VideoChat,
        /// with interruption / media-services-reset observers). Idempotent. Must be
        /// called before the native audio engine opens the VoiceProcessingIO audio
        /// unit; the SDK handles this automatically at the relevant call sites.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the session ended up VPIO-compatible and was activated;
        /// <c>false</c> otherwise (details under <c>[StreamVideo iOS Audio]</c> in the
        /// device log).
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
        public static bool ConfigureForWebRTC()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _StreamConfigureAudioSessionForWebRTC() == 1;
#else
            throw NotSupported(nameof(ConfigureForWebRTC));
#endif
        }

        /// <summary>
        /// Deactivate the AVAudioSession set up by <see cref="ConfigureForWebRTC"/> and
        /// notify other apps so background music / navigation can resume.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
        public static void DeconfigureAudioSession()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamDeconfigureAudioSession();
#else
            throw NotSupported(nameof(DeconfigureAudioSession));
#endif
        }

        /// <summary>
        /// Hard-override the output to the built-in loudspeaker even if a wired or
        /// Bluetooth headset is connected. Use sparingly: this overrides the user's
        /// headphone choice. <see cref="ConfigureForWebRTC"/> already defaults to the
        /// loudspeaker while still letting headphones take over automatically; only
        /// call this for an explicit speakerphone toggle.
        /// Use <see cref="ClearOutputOverride"/> to undo.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
        public static void ForceLoudspeaker()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamForceOutputToSpeaker();
#else
            throw NotSupported(nameof(ForceLoudspeaker));
#endif
        }

        /// <summary>
        /// Removes a previous <see cref="ForceLoudspeaker"/> override so iOS picks the
        /// route normally (headphones if connected, otherwise the built-in loudspeaker).
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
        public static void ClearOutputOverride()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamClearOutputOverride();
#else
            throw NotSupported(nameof(ClearOutputOverride));
#endif
        }

        /// <summary>
        /// Set microphone input gain to 1.0. iOS rarely allows this for the built-in
        /// mic; the call is best-effort and silently ignored when the system disallows
        /// it. <see cref="ConfigureForWebRTC"/> already invokes this.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
        public static void MaximizeInputGain()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _StreamMaximizeInputGain();
#else
            throw NotSupported(nameof(MaximizeInputGain));
#endif
        }

        /// <summary>
        /// <c>true</c> when the AVAudioSession is currently configured for the
        /// VoiceProcessingIO audio unit (i.e. <c>category == PlayAndRecord</c> and
        /// <c>mode == VoiceChat</c> or <c>VideoChat</c>), which gives us hardware
        /// AEC, noise suppression and automatic gain control.
        /// </summary>
        /// <remarks>
        /// This checks the session configuration only; it does NOT verify that the
        /// IO buffer duration is small enough for VPIO AEC to actually converge. See
        /// the <c>[StreamVideo iOS Audio] Configured: ... IOBuffer=...</c> device log
        /// line for the authoritative status.
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
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
        /// Returns a human-readable dump of the current iOS audio session state
        /// (category, mode, options, route, sample rate, IO buffer, latencies).
        /// Useful for diagnostics.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown on non-iOS targets.</exception>
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
