using Libs.DeviceManagers;

namespace StreamVideo.Libs.DeviceManagers
{
    /// <summary>
    /// iOS-specific audio device manager.
    /// </summary>
    /// <remarks>
    /// On iOS, the OS does not expose individual physical microphone devices the same way Android does.
    /// Audio input is implicitly tied to the active <c>AVAudioSession</c> route which is controlled by the system
    /// (with optional hints via <c>setPreferredInput</c> / <c>overrideOutputAudioPort</c>).
    ///
    /// For the first iteration we expose a single synthetic "Default Microphone" entry so that the SDK's device
    /// selection flow runs and ultimately triggers <c>StartLocalAudioCapture</c> on the publisher track. The
    /// underlying native miniaudio implementation uses the OS-default device, so the synthetic id is not consumed
    /// by the native plugin.
    ///
    /// StreamTODO: expose real iOS audio routing options (Built-in Mic, Bluetooth, Wired Headset, Speaker override,
    /// AirPods, etc.) by querying <c>AVAudioSession.availableInputs</c> from a native bridge and applying the
    /// chosen one via <c>setPreferredInput:</c>.
    /// </remarks>
    internal static class IOSAudioDeviceManager
    {
        private const int DefaultMicrophoneId = 0;
        private const string DefaultMicrophoneName = "Default Microphone";

        public static void GetAudioInputDevices(ref NativeAudioDeviceManager.AudioDeviceInfo[] result)
        {
            AudioDeviceManagerHelper.ClearBuffer(ref result);

            if (result == null || result.Length < 1)
            {
                result = new NativeAudioDeviceManager.AudioDeviceInfo[1];
            }

            result[0] = new NativeAudioDeviceManager.AudioDeviceInfo(DefaultMicrophoneId, DefaultMicrophoneName);
        }
    }
}
