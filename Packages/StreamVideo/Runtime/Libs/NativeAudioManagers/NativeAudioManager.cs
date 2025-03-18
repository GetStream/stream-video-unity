using System.Collections.Generic;
using StreamVideo.Libs.NativeAudioManagers;

namespace Libs.NativeAudioManagers
{
    public class NativeAudioManager : INativeAudioManager
    {
        public void GetAudioDebugInfo(IDictionary<string, string> result, out string error)
        {
#if UNITY_ANDROID
            AndroidAudioManager.ExecuteGetAudioDebugInfo(result, out error);
#else
            throw new System.NotSupportedException();
#endif
        }

        public void SetupAudioModeForVideoCall(out string error)
        {
#if UNITY_ANDROID
            AndroidAudioManager.ExecuteSetupAudioModeForVideoCall(out error);
#else
            throw new System.NotSupportedException();
#endif
        }
    }
}