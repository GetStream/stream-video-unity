using System.Collections.Generic;

namespace Libs.NativeAudioManagers
{
    public class EmptyNativeAudioManager : INativeAudioManager
    {
        public void GetAudioDebugInfo(IDictionary<string, string> result, out string error)
        {
            error = "Not implemented";
        }

        public void SetupAudioModeForVideoCall(out string error)
        {
            error = "Not implemented";
        }
    }
}