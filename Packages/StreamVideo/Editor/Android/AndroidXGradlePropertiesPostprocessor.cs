using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace StreamVideo.EditorTools.Android
{
    /// <summary>
    /// Background filters depend on ML Kit, which requires AndroidX. Gradle reads <c>android.useAndroidX</c> only from
    /// the root project's gradle.properties, which Unity generates. Unity 2022.3+ enables it, Unity 2021.3 does not.
    /// Properties that are already set are left untouched.
    /// </summary>
    internal class AndroidXGradlePropertiesPostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradlePropertiesPath = Path.GetFullPath(Path.Combine(path, "..", "gradle.properties"));
            if (!File.Exists(gradlePropertiesPath))
            {
                Debug.LogWarning($"[Stream Video] Could not find `{gradlePropertiesPath}`. If the Android build fails " +
                                 $"with an AndroidX error, set `{UseAndroidXKey}=true` in your gradle.properties.");
                return;
            }

            var contents = File.ReadAllText(gradlePropertiesPath);

            if (TryGetPropertyValue(contents, UseAndroidXKey, out var useAndroidX))
            {
                if (!string.Equals(useAndroidX, "true", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[Stream Video] `{UseAndroidXKey}` is set to `{useAndroidX}` in gradle.properties. " +
                                     $"Stream Video requires AndroidX on Android. Set `{UseAndroidXKey}=true` or the " +
                                     "Gradle build will fail.");
                }

                return;
            }

            var linesToAppend = UseAndroidXKey + "=true\n";

            // Jetifier keeps legacy Android Support Library plugins working once AndroidX is enabled.
            if (!TryGetPropertyValue(contents, EnableJetifierKey, out _))
            {
                linesToAppend += EnableJetifierKey + "=true\n";
            }

            if (contents.Length > 0 && !contents.EndsWith("\n"))
            {
                linesToAppend = "\n" + linesToAppend;
            }

            File.AppendAllText(gradlePropertiesPath, linesToAppend);
        }

        private const string UseAndroidXKey = "android.useAndroidX";
        private const string EnableJetifierKey = "android.enableJetifier";

        private static bool TryGetPropertyValue(string contents, string key, out string value)
        {
            var match = Regex.Match(contents, $@"^[ \t]*{Regex.Escape(key)}[ \t]*[=:][ \t]*(.*?)[ \t]*\r?$",
                RegexOptions.Multiline);
            value = match.Success ? match.Groups[1].Value : null;
            return match.Success;
        }
    }
}
