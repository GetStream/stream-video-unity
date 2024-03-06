using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal class AudioDeviceManager : DeviceManagerBase, IAudioDeviceManager
    {
        internal AudioDeviceManager(RtcSession rtcSession)
            : base(rtcSession)
        {
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetAudioTrackEnabled(isEnabled);
    }
}