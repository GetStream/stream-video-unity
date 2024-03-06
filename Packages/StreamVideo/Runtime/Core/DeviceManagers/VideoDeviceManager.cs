using StreamVideo.Core.LowLevelClient;

namespace StreamVideo.Core.DeviceManagers
{
    internal class VideoDeviceManager : DeviceManagerBase, IVideoDeviceManager
    {
        internal VideoDeviceManager(RtcSession rtcSession)
            : base(rtcSession)
        {
        }

        protected override void OnSetEnabled(bool isEnabled) => RtcSession.TrySetVideoTrackEnabled(isEnabled);
    }
}