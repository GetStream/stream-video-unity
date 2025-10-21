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
        /// When calling <see cref="GetAudioInputDevices"/>, the returned devices Ids represent audio routing options.
        /// </summary>
        /// <param name="audioRoute"></param>
        public static void SetPreferredAudioRoute(NativeAudioDeviceManager.AudioRouting audioRoute)
        {
            CallStatic("setAudioRoute", (int)audioRoute);
        }
        
        /// <summary>
        /// For Android, the devices represent available audio routing options instead of physical devices.
        /// </summary>
        /// <param name="result"></param>
        public static void GetAudioInputDevices(ref NativeAudioDeviceManager.AudioDeviceInfo[] result)
            => GetAudioInputDevices(ref result, ref _audioInputDevicesBuffer);
        
        private static void GetAudioInputDevices(ref NativeAudioDeviceManager.AudioDeviceInfo[] result,
            ref string[] internalBuffer)
        {
            AudioDeviceManagerHelper.ClearBuffer(ref internalBuffer);
            AudioDeviceManagerHelper.ClearBuffer(ref result);
        
            #if UNITY_IOS
            var deviceIndex = 0;
            foreach (var device in Microphone.devices)
            {
                var name = Microphone.devices[deviceIndex];
                result[deviceIndex] = new NativeAudioDeviceManager.AudioDeviceInfo(deviceIndex, name);
            }


            return;
            #endif
            
            
            var javaArray = CallStatic<AndroidJavaObject>("getAvailableAudioInputDevices");
        
            AndroidJavaArrayToStringArray(javaArray, ref internalBuffer);
        
            var index = 0;
            foreach (var entry in internalBuffer)
            {
                if (entry == null || !ParseDeviceString(entry, out var deviceInfo))
                {
                    continue;
                }
        
                result[index++] = deviceInfo;
            }
        }

        // Java class name with full namespace. This file needs to be included in the .aar file
        private const string AndroidAudioWrapperJavaClassFullPath
            = "io.getstream.unityaudiomanagerwrapper.UnityAudioManagerWrapper";

        private static string[] _audioInputDevicesBuffer = new string[128];

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

        private static bool ParseDeviceString(string deviceString, out NativeAudioDeviceManager.AudioDeviceInfo audioDeviceInfo)
        {
            var parts = deviceString.Split('|');
            if (parts.Length < 2)
            {
                Debug.LogError("Invalid device string format: " + deviceString);
                audioDeviceInfo = default;
                return false;
            }

            var id = int.Parse(parts[0]);
            var name = parts[1];

            audioDeviceInfo = new NativeAudioDeviceManager.AudioDeviceInfo(
                id,
                name
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
        
        private static void CallStatic(string methodName, params object[] args)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var helperClass = new AndroidJavaClass(AndroidAudioWrapperJavaClassFullPath))
            {
                var fullArgs = new object[args.Length + 1];
                fullArgs[0] = currentActivity;
                Array.Copy(args, 0, fullArgs, 1, args.Length);
                helperClass.CallStatic(methodName, fullArgs);
            }
        }
    }
}