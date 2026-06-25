using Libs.DeviceManagers;

namespace StreamVideo.Libs.DeviceManagers
{
    /// <summary>
    /// iOS audio input device manager. Exposes a single synthetic "Default Microphone"
    /// entry; the actual mic is picked by the system based on the AVAudioSession route.
    /// The synthetic id is not consumed by the native plugin (miniaudio uses the OS-default device).
    /// StreamTODO: expose real routing options (Built-in / Bluetooth / Wired / AirPods)
    /// by querying <c>AVAudioSession.availableInputs</c> and applying via <c>setPreferredInput:</c>.
    /// </summary>
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
