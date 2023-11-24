using System;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor.BuildPostprocess
{
    /// <summary>
    /// This script will automatically apply the necessary build settings required by the Stream's Video & Audio SDK for Android
    /// </summary>
    internal class AndroidBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                try
                {
                    ConfigureAndroidBuildSettings();
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        "Failed to apply Stream's Video & Audio SDK build settings for the Android platform. " +
                        "Please check the documentation and apply the necessary changes manually. Error: " + e.Message);
                }
            }
        }

        private static void ConfigureAndroidBuildSettings()
        {
            var sb = new StringBuilder();

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                // Set Scripting Backend to IL2CPP
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

                sb.AppendLine("- Changed scripting backend to IL2CPP");
            }

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                // Set Target Architectures to ARM64 and disable ARMv7
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                sb.AppendLine("- Changed target architecture to ARM64 only (ARMv7 is not supported)");
            }

            if (PlayerSettings.Android.forceInternetPermission != true)
            {
                // Set Internet Access requirement
                PlayerSettings.Android.forceInternetPermission = true;

                sb.AppendLine("- Changed internet permission to be required");
            }

            // Set the Android API Level to 23 or higher
            const int minApiLevel = (int)AndroidSdkVersions.AndroidApiLevel23;
            if ((int)PlayerSettings.Android.minSdkVersion < minApiLevel)
            {
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)minApiLevel;

                sb.AppendLine($"- Changed minimum Android SDK version to `{minApiLevel}`");
            }

            if (sb.Length > 0)
            {
                Debug.Log(
                    "Stream Video & Audio SDK - Build Preprocess - Successfully applied settings required by the SDK. Details below: \n" +
                    sb);
            }
        }
    }
}