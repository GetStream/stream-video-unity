using System;
using Libs.DeviceManagers;
using UnityEngine;

namespace StreamVideo.Libs.DeviceManagers
{
    /// <summary>
    /// Wrapper for Java native class that provides access to Android's AudioManager
    /// </summary>
    /// <remarks>
    /// This outputs Android's physical devices. This is not the same as Unity's Microphone devices because Unity exposes multiple recording profiles: audio input, camcorder input, voice recognition input
    /// These profiles point to the same physical device. StreamTODO: consider exposing these profiles as well. For starter, user could select if recording should be optimized for voice or not.
    /// </remarks>
    internal static class AndroidAudioDeviceManager
    {
        /// <summary>
        /// This maps int types defined here: https://developer.android.com/reference/android/media/AudioDeviceInfo#constants_1
        /// </summary>
        public enum NativeDeviceType
        {
            Unknown = 0,
            BuiltinEarpiece = 1,
            BuiltinSpeaker = 2,
            WiredHeadset = 3,
            WiredHeadphones = 4,
            LineAnalog = 5,
            LineDigital = 6,
            BluetoothSCO = 7,
            BluetoothA2DP = 8,
            HDMI = 9,
            HDMIARC = 10,
            USBDevice = 11,
            USBAccessory = 12,
            Dock = 13,
            FM = 14,
            BuiltinMic = 15,
            FMTuner = 16,
            TVTuner = 17,
            Telephony = 18,
            AuxiliaryLine = 19,
            IP = 20,
            Bus = 21,
            USBHeadset = 22,
            HearingAid = 23,
            BuiltinSpeakerSafe = 24,
            RemoteSubmix = 25,
            BluetoothLEHeadset = 26,
            BluetoothLESpeaker = 27,
            HDMI_EARC = 29,
            BluetoothLEBroadcast = 30,
            DockAnalog = 31,
            MultichannelGroup = 32,
        }

        public static void GetAudioInputDevices(ref AudioDeviceManager.AudioDeviceInfo[] result)
            => GetAudioInputDevices(ref result, ref _audioInputDevicesBuffer, AudioDeviceManager.Direction.Input);

        public static void GetAudioOutputDevices(ref AudioDeviceManager.AudioDeviceInfo[] result)
            => GetAudioInputDevices(ref result, ref _audioOutputDevicesBuffer, AudioDeviceManager.Direction.Output);

        // Java class name with full namespace. This file needs to be included in the .aar file
        private const string AndroidAudioWrapperJavaClassFullPath
            = "io.getstream.unityaudiomanagerwrapper.UnityAudioManagerWrapper";

        private static string[] _audioInputDevicesBuffer = new string[64];
        private static string[] _audioOutputDevicesBuffer = new string[64];

        private static void GetAudioInputDevices(ref AudioDeviceManager.AudioDeviceInfo[] result,
            ref string[] internalBuffer, AudioDeviceManager.Direction direction)
        {
            AudioDeviceManagerHelper.ClearBuffer(ref internalBuffer);
            AudioDeviceManagerHelper.ClearBuffer(ref result);

            var methodName = direction == AudioDeviceManager.Direction.Input
                ? "getAudioInputDevices"
                : "getAudioOutputDevices";
            var javaArray = CallStatic<AndroidJavaObject>(methodName);

            AndroidJavaArrayToStringArray(javaArray, ref internalBuffer);

            var index = 0;
            foreach (var entry in internalBuffer)
            {
                if (entry == null || !ParseDeviceString(entry, direction, out var deviceInfo))
                {
                    continue;
                }

                result[index++] = deviceInfo;
            }
        }

        private static void AndroidJavaArrayToStringArray(AndroidJavaObject javaArray, ref string[] result)
        {
            var arrayPtr = javaArray.GetRawObject();
            var length = AndroidJNI.GetArrayLength(arrayPtr);

            if (result == null || result.Length < length)
            {
                result = new string[AudioDeviceManagerHelper.FindClosestPowerOfTwo(length)];
            }

            for (var i = 0; i < length; i++)
            {
                // Creates local reference to Java object
                var element = AndroidJNI.GetObjectArrayElement(arrayPtr, i);
                result[i] = AndroidJNI.GetStringUTFChars(element);

                // Deletes local reference to avoid memory leaks
                AndroidJNI.DeleteLocalRef(element);
            }
        }

        private static bool ParseDeviceString(string deviceString, AudioDeviceManager.Direction direction,
            out AudioDeviceManager.AudioDeviceInfo audioDeviceInfo)
        {
            var parts = deviceString.Split(':');
            if (parts.Length < 7)
            {
                Debug.LogError("Invalid device string format: " + deviceString);
                audioDeviceInfo = default;
                return false;
            }

            var id = int.Parse(parts[0]);
            var name = parts[1];
            var friendlyName = parts[2];
            var deviceType = (NativeDeviceType)Enum.Parse(typeof(NativeDeviceType), parts[3]);
            var channelCounts = Array.ConvertAll(parts[4].Split('|'), int.Parse);
            var sampleRates = Array.ConvertAll(parts[5].Split('|'), int.Parse);
            var encodings = Array.ConvertAll(parts[6].Split('|'), int.Parse);

            audioDeviceInfo = new AudioDeviceManager.AudioDeviceInfo(
                id,
                name,
                friendlyName,
                direction,
                GetAudioDeviceType(deviceType),
                channelCounts,
                sampleRates,
                encodings,
                deviceString + ", NativeType:" + Enum.GetName(typeof(NativeDeviceType), deviceType)
            );

            return true;
        }

        private static TReturn CallStatic<TReturn>(string methodName)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var helperClass = new AndroidJavaClass(AndroidAudioWrapperJavaClassFullPath))
            {
                return helperClass.CallStatic<TReturn>(methodName, currentActivity);
            }
        }

        private static AudioDeviceManager.AudioDeviceType GetAudioDeviceType(NativeDeviceType nativeType)
        {
            switch (nativeType)
            {
                case NativeDeviceType.Unknown:
                    return AudioDeviceManager.AudioDeviceType.Unknown;
                case NativeDeviceType.BuiltinEarpiece:
                    return AudioDeviceManager.AudioDeviceType.BuiltinEarpiece;
                case NativeDeviceType.BuiltinSpeaker:
                    return AudioDeviceManager.AudioDeviceType.BuiltinSpeaker;
                case NativeDeviceType.WiredHeadset:
                case NativeDeviceType.WiredHeadphones:
                case NativeDeviceType.LineAnalog:
                case NativeDeviceType.LineDigital:
                    return AudioDeviceManager.AudioDeviceType.Other;
                case NativeDeviceType.BluetoothSCO:
                case NativeDeviceType.BluetoothA2DP:
                    return AudioDeviceManager.AudioDeviceType.Bluetooth;
                case NativeDeviceType.HDMI:
                case NativeDeviceType.HDMIARC:
                case NativeDeviceType.USBDevice:
                case NativeDeviceType.USBAccessory:
                case NativeDeviceType.Dock:
                case NativeDeviceType.FM:
                    return AudioDeviceManager.AudioDeviceType.Other;
                case NativeDeviceType.BuiltinMic:
                    return AudioDeviceManager.AudioDeviceType.BuiltinMic;
                case NativeDeviceType.FMTuner:
                case NativeDeviceType.TVTuner:
                case NativeDeviceType.Telephony:
                case NativeDeviceType.AuxiliaryLine:
                case NativeDeviceType.IP:
                case NativeDeviceType.Bus:
                case NativeDeviceType.USBHeadset:
                case NativeDeviceType.HearingAid:
                    return AudioDeviceManager.AudioDeviceType.Other;
                case NativeDeviceType.BuiltinSpeakerSafe:
                    return AudioDeviceManager.AudioDeviceType.BuiltinSpeaker;
                case NativeDeviceType.RemoteSubmix:
                case NativeDeviceType.BluetoothLEHeadset:
                case NativeDeviceType.BluetoothLESpeaker:
                case NativeDeviceType.HDMI_EARC:
                case NativeDeviceType.BluetoothLEBroadcast:
                case NativeDeviceType.DockAnalog:
                case NativeDeviceType.MultichannelGroup:
                    return AudioDeviceManager.AudioDeviceType.Other;
                default:
                    return AudioDeviceManager.AudioDeviceType.Unknown;
            }
        }
    }
}