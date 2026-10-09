#if STREAM_TESTS_ENABLED
using System;
using System.IO;
using NUnit.Framework;
using StreamVideo.EditorTools.Android;
using UnityEditor.Build;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="AndroidCompileSdkValidator"/>.
    /// </summary>
    internal sealed class AndroidCompileSdkValidatorTests
    {
        [SetUp]
        public void SetUp()
        {
            _gradleProjectDirectory = Path.Combine(Path.GetTempPath(), "StreamVideoGradle_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_gradleProjectDirectory, UnityLibraryModule));
            Directory.CreateDirectory(Path.Combine(_gradleProjectDirectory, LauncherModule));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_gradleProjectDirectory))
            {
                Directory.Delete(_gradleProjectDirectory, recursive: true);
            }
        }

        [TestCase("    compileSdkVersion 30", 30)]
        [TestCase("    compileSdk 35", 35)]
        [TestCase("    compileSdk = 34", 34)]
        [TestCase("    compileSdkVersion = 33", 33)]
        [TestCase("    compileSdkVersion(31)", 31)]
        [TestCase("    compileSdkVersion 'android-30'", 30)]
        [TestCase("    compileSdkVersion \"android-29\"", 29)]
        public void When_compile_sdk_is_literal_expect_parsed(string line, int expected)
        {
            var parsed = AndroidCompileSdkValidator.TryParseCompileSdk(WrapInAndroidBlock(line), out var compileSdk);

            Assert.That(parsed, Is.True, $"Expected `{line}` to be parsed.");
            Assert.That(compileSdk, Is.EqualTo(expected), $"Wrong API level parsed from `{line}`.");
        }

        [TestCase("    // compileSdkVersion 30")]
        [TestCase("    compileSdkVersion rootProject.ext.compileSdk")]
        [TestCase("    compileSdkPreview 'UpsideDownCake'")]
        [TestCase("    compileSdkExtension 7")]
        [TestCase("    minSdkVersion 23")]
        public void When_compile_sdk_is_not_a_literal_expect_not_parsed(string line)
        {
            var parsed = AndroidCompileSdkValidator.TryParseCompileSdk(WrapInAndroidBlock(line), out _);

            Assert.That(parsed, Is.False, $"`{line}` must not be treated as a compile SDK value.");
        }

        [Test]
        public void When_launcher_compile_sdk_below_minimum_expect_build_failed()
        {
            WriteBuildGradle(UnityLibraryModule, AndroidCompileSdkValidator.MinCompileSdk);
            WriteBuildGradle(LauncherModule, 30);

            var exception = Assert.Throws<BuildFailedException>(Validate,
                "A launcher compiled against an API level below the minimum must fail the build.");
            Assert.That(exception.Message, Does.Contain("launcher/build.gradle"),
                "The error must name the Gradle file with the too-low compile SDK.");
        }

        [Test]
        public void When_unity_library_compile_sdk_below_minimum_expect_build_failed()
        {
            WriteBuildGradle(UnityLibraryModule, 30);
            WriteBuildGradle(LauncherModule, AndroidCompileSdkValidator.MinCompileSdk);

            Assert.Throws<BuildFailedException>(Validate,
                "unityLibrary compiled against an API level below the minimum must fail the build.");
        }

        [Test]
        public void When_compile_sdk_at_or_above_minimum_expect_no_failure()
        {
            WriteBuildGradle(UnityLibraryModule, AndroidCompileSdkValidator.MinCompileSdk);
            WriteBuildGradle(LauncherModule, AndroidCompileSdkValidator.MinCompileSdk + 2);

            Assert.DoesNotThrow(Validate, "A compile SDK at or above the minimum must not fail the build.");
        }

        [Test]
        public void When_build_gradle_files_missing_expect_no_failure()
        {
            Assert.DoesNotThrow(Validate, "Missing Gradle files are left for Gradle to report.");
        }

        private const string UnityLibraryModule = "unityLibrary";
        private const string LauncherModule = "launcher";

        private string _gradleProjectDirectory;

        private static string WrapInAndroidBlock(string line) => "android {\n" + line + "\n    buildToolsVersion '30.0.2'\n}\n";

        private void WriteBuildGradle(string module, int compileSdk)
            => File.WriteAllText(Path.Combine(_gradleProjectDirectory, module, "build.gradle"),
                WrapInAndroidBlock($"    compileSdkVersion {compileSdk}"));

        private void Validate()
            => new AndroidCompileSdkValidator().OnPostGenerateGradleAndroidProject(
                Path.Combine(_gradleProjectDirectory, UnityLibraryModule));
    }
}
#endif
