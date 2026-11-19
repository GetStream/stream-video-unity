#if STREAM_TESTS_ENABLED
using System.Collections.Generic;
using NUnit.Framework;
using StreamVideo.EditorTools.Builders;
using StreamVideo.EditorTools.CommandLineParsers;
using UnityEditor;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="BuildSettingsCommandLineParser"/>.
    /// </summary>
    internal sealed class BuildSettingsCommandLineParserTests
    {
        [Test]
        public void When_required_build_args_are_present_expect_build_settings()
        {
            var parser = new ExposedBuildSettingsCommandLineParser();
            var args = new Dictionary<string, string>
            {
                { BuildSettingsCommandLineParser.BuildTargetPlatformArgKey, "Android" },
                { BuildSettingsCommandLineParser.ApiCompatibilityArgKey, "NET_4_x" },
                { BuildSettingsCommandLineParser.ScriptingBackendArgKey, "IL2CPP" },
                { BuildSettingsCommandLineParser.BuildTargetPathArgKey, "SampleAppBuild/app.apk" },
            };

            var buildSettings = parser.ParseArgs(args);

            Assert.That(buildSettings.BuildTargetGroup, Is.EqualTo(BuildTargetGroup.Android),
                "Sample build should parse its target.");
            Assert.That(buildSettings.ScriptingImplementation, Is.EqualTo(ScriptingImplementation.IL2CPP),
                "Sample build should parse its scripting backend.");
            Assert.That(buildSettings.TargetPath, Is.EqualTo("SampleAppBuild/app.apk"),
                "Sample build should parse its output path.");
        }

        private sealed class ExposedBuildSettingsCommandLineParser : BuildSettingsCommandLineParser
        {
            public BuildSettings ParseArgs(IDictionary<string, string> args) => Parse(args);
        }
    }
}
#endif
