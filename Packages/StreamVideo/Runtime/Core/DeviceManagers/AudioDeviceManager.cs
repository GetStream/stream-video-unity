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
        
        //StreamTodo: wrap all operations on the Microphone devices + monitor for devices list changes
        //We could also allow to smart pick device -> sample each device and check which of them are actually gathering any input
    }
}