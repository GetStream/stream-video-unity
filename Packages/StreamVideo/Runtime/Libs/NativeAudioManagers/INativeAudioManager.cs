using System.Collections.Generic;

namespace Libs.NativeAudioManagers
{
    public interface INativeAudioManager
    {
        void GetAudioDebugInfo(IDictionary<string, string> result, out string error);

        void SetupAudioModeForVideoCall(out string error);
    }
}