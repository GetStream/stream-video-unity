using System;
using System.Collections.Generic;
using UnityEngine;

namespace StreamVideo.Libs.NativeAudioManagers
{
    internal static class AndroidAudioManager
    {
        public static void ExecuteSetupAudioModeForVideoCall(out string error)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                error = "Not running on Android platform";
            }
#if UNITY_ANDROID
            error = PluginClass.CallStatic<string>("SetupAudioModeForVideoCall");
#else
            error = "Not compiled for Android";
#endif
        }

        public static void ExecuteGetAudioDebugInfo(IDictionary<string, string> result, out string error)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                error = "Not running on Android platform";
                return;
            }

#if UNITY_ANDROID
            try
            {
                var debugInfoString = PluginClass.CallStatic<string>("GetAudioDebugInfo");
                ParseDebugInfo(debugInfoString, result, out error);
            }
            catch (Exception e)
            {
                error = "Failed to get & parse debug info with exception: " + e.Message;
            }
#else
            error = "Not compiled for Android";
#endif
        }
        
        private static AndroidJavaClass _pluginClass;

        private static AndroidJavaClass PluginClass
        {
            get
            {
                if (_pluginClass == null)
                {
                    _pluginClass = new AndroidJavaClass("com.stream.audioutils.StreamAndroidAudioPlugin");
                }

                return _pluginClass;
            }
        }

        private static void ParseDebugInfo(string debugInfoString, IDictionary<string, string> result, out string error)
        {
            if (string.IsNullOrEmpty(debugInfoString))
            {
                error = "No debug info returned";
                return;
            }

            var pairs = debugInfoString.Split('|');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(new char[] { '=' }, 2);
                if (keyValue.Length == 2)
                {
                    result.Add(keyValue[0], keyValue[1]);
                }
            }

            error = string.Empty;

#if STREAM_DEBUG_ENABLED
            // Log everything to console
            Debug.Log("===== ANDROID AUDIO DEBUG INFO =====");
            foreach (var item in result)
            {
                Debug.Log($"{item.Key} = {item.Value}");
            }
#endif
        }
    }
}