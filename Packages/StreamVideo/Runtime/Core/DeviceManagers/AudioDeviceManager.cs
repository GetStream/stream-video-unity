using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using UnityEngine;

namespace StreamVideo.Core.DeviceManagers
{
    internal class AudioDeviceManager : DeviceManagerBase<MicrophoneDeviceInfo>, IAudioDeviceManager
    {
        internal AudioDeviceManager(RtcSession rtcSession)
            : base(rtcSession)
        {
        }

        public override IEnumerable<MicrophoneDeviceInfo> EnumerateDevices()
        {
            foreach (var device in Microphone.devices)
            {
                yield return new MicrophoneDeviceInfo(device);
            }
        }

        public override Task<bool> IsDeviceStreamingAsync(MicrophoneDeviceInfo device)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetAudioTrackEnabled(isEnabled);
        
        //StreamTodo: wrap all operations on the Microphone devices + monitor for devices list changes
        //We could also allow to smart pick device -> sample each device and check which of them are actually gathering any input
    }
}