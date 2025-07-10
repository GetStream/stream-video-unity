using System;

namespace StreamVideo.Libs.DeviceManagers
{
    // StreamTodo: implement detecting when device list changed so we can be notified about devices being added/removed and not have to query the list periodically.
    public static class NativeAudioDeviceManager
    {
        public readonly struct AudioDeviceInfo : IEquatable<AudioDeviceInfo>
        {
            public readonly int Id;
            public readonly string Name;

            public AudioDeviceInfo(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString() => $"Audio Device -> ID: {Id}, Name: {Name}";

            public bool Equals(AudioDeviceInfo other)
            {
                return Id == other.Id &&
                       Name == other.Name;
            }

            public override bool Equals(object obj)
            {
                return obj is AudioDeviceInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + Id;
                    hash = hash * 23 + (Name != null ? Name.GetHashCode() : 0);
                    return hash;
                }
            }

            public static bool operator ==(AudioDeviceInfo left, AudioDeviceInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AudioDeviceInfo left, AudioDeviceInfo right)
            {
                return !left.Equals(right);
            }
        }

        /// <summary>
        /// This maps to cases in the `setAudioRoute` Java method in the UnityAudioManagerWrapper java class. (AndroidAudioManagerWrapper.aar file)
        /// </summary>
        public enum AudioRouting
        {
            Earpiece = 0,
            Speaker = 1,
            Bluetooth = 2,
        }
        
        public static void SetPreferredAudioRoute(AudioRouting audioRoute)
        {
#if UNITY_ANDROID
            AndroidAudioDeviceManager.SetPreferredAudioRoute(audioRoute);
#else
            UnityEngine.Debug.LogWarning($"{nameof(GetAudioInputDevices)} is not supported on this platform: " + Application.platform);
#endif
        }

        public static void GetAudioInputDevices(ref AudioDeviceInfo[] result)
        {
#if UNITY_ANDROID
            AndroidAudioDeviceManager.GetAudioInputDevices(ref result);
#else
            UnityEngine.Debug.LogWarning($"{nameof(GetAudioInputDevices)} is not supported on this platform: " + Application.platform);
#endif
        }
    }
}