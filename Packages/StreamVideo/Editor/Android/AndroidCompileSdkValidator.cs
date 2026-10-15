using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEditor.Build;

namespace StreamVideo.EditorTools.Android
{
    /// <summary>
    /// Fails the Android build when the generated Gradle project compiles against an API level lower than
    /// <see cref="MinCompileSdk"/>. Background filters depend on ML Kit, which pulls in androidx.core 1.9.0, and that
    /// requires compiling against API 33 or higher. Android Gradle Plugin 4 (Unity 2021.3.40 and older) does not enforce
    /// this and fails later with an unclear `resource android:attr/lStar not found` error.
    /// The Gradle project is only read, never modified.
    /// </summary>
    internal class AndroidCompileSdkValidator : IPostGenerateGradleAndroidProject
    {
        public const int MinCompileSdk = 33;

        // Run last so a compile SDK raised by another post-processor is what gets validated.
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Validate(Path.GetFullPath(path));
            Validate(Path.GetFullPath(Path.Combine(path, "..", "launcher")));
        }

        internal static bool TryParseCompileSdk(string gradleContents, out int compileSdk)
        {
            compileSdk = 0;
            var match = CompileSdkRegex.Match(gradleContents);
            return match.Success && int.TryParse(match.Groups[1].Value, out compileSdk);
        }

        private static readonly Regex CompileSdkRegex = new Regex(
            @"^[ \t]*compileSdk(?:Version)?(?:[ \t]*=[ \t]*|[ \t]*\([ \t]*|[ \t]+)['""]?(?:android-)?(\d+)",
            RegexOptions.Multiline);

        private static void Validate(string moduleDirectory)
        {
            var buildGradlePath = Path.Combine(moduleDirectory, "build.gradle");
            if (!File.Exists(buildGradlePath)
                || !TryParseCompileSdk(File.ReadAllText(buildGradlePath), out var compileSdk)
                || compileSdk >= MinCompileSdk)
            {
                return;
            }

            throw new BuildFailedException(
                $"[Stream Video] The Android build compiles against API level {compileSdk} " +
                $"(`{Path.GetFileName(moduleDirectory)}/build.gradle`), but Stream Video requires API level " +
                $"{MinCompileSdk} or higher. AndroidX libraries used by background filters (androidx.core 1.9.0) " +
                "cannot be compiled against older API levels. To fix it, either set Player Settings > Other Settings > " +
                $"Target API Level to {MinCompileSdk} or higher, or keep it on Automatic and install Android SDK " +
                $"Platform {MinCompileSdk} or higher with the Android SDK Manager. If a custom Gradle template sets " +
                "compileSdkVersion, raise it there.");
        }
    }
}
