using UnityEngine;

namespace Libs.iOSAudioManagers
{
    public class IOSAudioManager
    {
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern IntPtr GetAudioSessionInfo();
    
    [DllImport("__Internal")]
    private static extern void SetAudioModeDefault();
    
    [DllImport("__Internal")]
    private static extern void SetAudioModeVoiceChat();
    
    [DllImport("__Internal")]
    private static extern void SetAudioModeVideoChat();
    
    [DllImport("__Internal")]
    private static extern void ConfigureAudioSessionForWebRTC();
#endif

        public enum AudioMode
        {
            Default,      // No voice processing
            VoiceChat,    // AEC/AGC/NS enabled - optimized for voice
            VideoChat     // AEC/AGC/NS enabled - optimized for video calls
        }
    
        /// <summary>
        /// Get current audio session configuration info
        /// </summary>
        public static string GetCurrentSettings()
        {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            IntPtr ptr = GetAudioSessionInfo();
            string info = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeHGlobal(ptr); // Free the strdup memory
            return info;
        }
        catch (Exception e)
        {
            return $"Error getting audio info: {e.Message}";
        }
#else
            return "iOS Audio Session Info (Only available on iOS device)";
#endif
        }
    
        /// <summary>
        /// Set audio mode
        /// </summary>
        public static void SetMode(AudioMode mode)
        {
#if UNITY_IOS && !UNITY_EDITOR
        switch (mode)
        {
            case AudioMode.Default:
                SetAudioModeDefault();
                Debug.Log("iOS Audio Mode: Default (NO voice processing)");
                break;
                
            case AudioMode.VoiceChat:
                SetAudioModeVoiceChat();
                Debug.Log("iOS Audio Mode: VoiceChat (AEC/AGC/NS ENABLED)");
                break;
                
            case AudioMode.VideoChat:
                SetAudioModeVideoChat();
                Debug.Log("iOS Audio Mode: VideoChat (AEC/AGC/NS ENABLED)");
                break;
        }
#else
            Debug.Log($"iOS Audio Mode set to: {mode} (simulation, not on iOS)");
#endif
        }
    
        /// <summary>
        /// Quick setup for WebRTC (uses VideoChat mode)
        /// </summary>
        public static void ConfigureForWebRTC()
        {
            SetMode(AudioMode.VideoChat);
        }
    
        /// <summary>
        /// Log current settings to console
        /// </summary>
        public static void LogCurrentSettings()
        {
            string info = GetCurrentSettings();
            Debug.Log(info);
        }
    }
}