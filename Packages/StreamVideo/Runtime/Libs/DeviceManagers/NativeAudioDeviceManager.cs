using UnityEngine;

namespace StreamVideo.Libs.DeviceManagers
{
    // StreamTodo: implement detecting when device list changed so we can be notified about devices being added/removed and not have to query the list periodically.
    public static class NativeAudioDeviceManager
    {
        public struct AudioDeviceInfo
        {
            public readonly int Id;
            public readonly string Name;
            public readonly string FriendlyName;
            public readonly Direction Direction;
            public readonly AudioDeviceType Type;
            public readonly int[] ChannelCounts;
            public readonly int[] SampleRates;
            public readonly int[] Encodings;
            public readonly string DebugInfo;

            public bool IsValid => Id > 0;

            public AudioDeviceInfo(int id, string name, string friendlyName, Direction direction, AudioDeviceType type, int[] channelCounts, int[] sampleRates, int[] encodings) 
                : this(id, name, friendlyName, direction, type, channelCounts, sampleRates, encodings, string.Empty)
            {
            }

            public AudioDeviceInfo(int id, string name, string friendlyName, Direction direction, AudioDeviceType type, int[] channelCounts, int[] sampleRates, int[] encodings, string debugInfo)
            {
                Id = id;
                Name = name;
                FriendlyName = friendlyName;
                Direction = direction;
                Type = type;
                ChannelCounts = channelCounts;
                SampleRates = sampleRates;
                Encodings = encodings;
                DebugInfo = debugInfo;
            }

            public override string ToString()
                => $"Audio Device -> ID: {Id}, Name: {Name}, Friendly Name: {FriendlyName}, Direction: {Direction}, Type: {Type}, " +
                   $"Channel counts: {PrintArray(ChannelCounts)}, Sample Rates: {PrintArray(SampleRates)}, Encodings: {PrintArray(Encodings)}, Debug Info: {DebugInfo}";

            private static string PrintArray(int[] array) => string.Join(", ", array);
        }

        public enum AudioDeviceType
        {
            Unknown,
            BuiltinEarpiece,
            BuiltinSpeaker,
            BuiltinMic,
            Bluetooth,
            Other
        }

        public enum Direction
        {
            Input,
            Output
        }

        public static bool IsPlatformSupported(RuntimePlatform platform) => platform == RuntimePlatform.Android;

        public static void GetAudioInputDevices(ref AudioDeviceInfo[] result)
        {
#if UNITY_ANDROID
            AndroidAudioDeviceManager.GetAudioInputDevices(ref result);
#else
            UnityEngine.Debug.LogWarning($"{nameof(GetAudioInputDevices)} is not supported on this platform: " + Application.platform);
#endif
        }

        public static void GetAudioOutputDevices(ref AudioDeviceInfo[] result)
        {
#if UNITY_ANDROID
            AndroidAudioDeviceManager.GetAudioOutputDevices(ref result);
#else
            UnityEngine.Debug.LogWarning($"{nameof(GetAudioOutputDevices)} is not supported on this platform: " + Application.platform);
#endif
        }
    }
}