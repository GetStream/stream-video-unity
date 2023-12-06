using System;
using System.Collections.Generic;
using StreamVideo.EditorTools.Builders;
using UnityEditor;

namespace StreamVideo.EditorTools
{
    public class CommandLineParser
    {
        public const string ApiCompatibilityArgKey = "-apiCompatibility";
        public const string ScriptingBackendArgKey = "-scriptingBackend";
        public const string BuildTargetPlatformArgKey = "-buildTargetPlatform";
        public const string BuildTargetPathArgKey = "-buildTargetPath";

        public BuildSettings GetParsedBuildArgs()
        {
            var args = GetParsedCommandLineArguments();

            if (IsAnyRequiredArgMissing(args, out var missingArgsInfo, BuildTargetPlatformArgKey,
                ApiCompatibilityArgKey, ScriptingBackendArgKey, BuildTargetPathArgKey))
            {
                throw new ArgumentException($"Missing argument: `{missingArgsInfo}`");
            }

            if (!Enum.TryParse<BuildTargetPlatform>(args[BuildTargetPlatformArgKey], ignoreCase: true,
                out var targetPlatform))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[BuildTargetPlatformArgKey]}` to enum: {typeof(BuildTargetPlatform)}");
            }

            if (!Enum.TryParse<ApiCompatibility>(args[ApiCompatibilityArgKey], ignoreCase: true,
                out var apiCompatibility))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[ApiCompatibilityArgKey]}` to enum: {typeof(ApiCompatibility)}");
            }

            if (!Enum.TryParse<ScriptingBackend>(args[ScriptingBackendArgKey], ignoreCase: true,
                out var scriptingBackend))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[ScriptingBackendArgKey]}` to enum: {typeof(ScriptingBackend)}");
            }

            var buildTargetGroup = GetBuildTargetGroup(targetPlatform);
            var apiCompatibilityLevel = GetApiCompatibilityLevel(apiCompatibility);
            var scriptingImplementation = GetScriptingImplementation(scriptingBackend);
            var targetPath = args[BuildTargetPathArgKey];

            return new BuildSettings(buildTargetGroup, apiCompatibilityLevel, scriptingImplementation, targetPath);
        }

        public IDictionary<string, string> GetParsedCommandLineArguments()
        {
            var result = new Dictionary<string, string>();
            ParseCommandLineArguments(Environment.GetCommandLineArgs(), result);

            return result;
        }

        public void ParseCommandLineArguments(string[] args, IDictionary<string, string> result)
            => ParseCommandLineArguments(args, _ => result.Add(_.Key, _.Value));

        public void ParseCommandLineArguments(string[] args, Action<(string Key, string Value)> onArgumentParsed)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    var key = args[i];
                    var value = i < args.Length - 1 ? args[i + 1] : "";

                    onArgumentParsed?.Invoke((key, value));
                }
            }
        }

        private static bool IsAnyRequiredArgMissing(IDictionary<string, string> args, out string missingArgsInfo,
            params string[] argKeys)
        {
            var missingKeys = new List<string>();

            foreach (var key in argKeys)
            {
                if (!args.ContainsKey(key))
                {
                    missingKeys.Add(key);
                }
            }

            missingArgsInfo = missingKeys.Count == 0 ? string.Empty : string.Join(", ", missingKeys);
            return missingKeys.Count != 0;
        }

        private BuildTargetGroup GetBuildTargetGroup(BuildTargetPlatform targetPlatform)
        {
            if (targetPlatform == BuildTargetPlatform.Standalone)
            {
                return BuildTargetGroup.Standalone;
            }

#if UNITY_STANDALONE_OSX
            return BuildTargetGroup.iOS;
#else
            return BuildTargetGroup.Android;
#endif
        }

        private ApiCompatibilityLevel GetApiCompatibilityLevel(ApiCompatibility apiCompatibility)
        {
#if UNITY_2021

            switch (apiCompatibility)
            {
                case ApiCompatibility.NET_4_x: return ApiCompatibilityLevel.NET_Unity_4_8;
                case ApiCompatibility.STANDARD_2_x: return ApiCompatibilityLevel.NET_Standard_2_0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(apiCompatibility), apiCompatibility, null);
            }

#else
            switch (apiCompatibility)
            {
                case ApiCompatibility.NET_4_x: return ApiCompatibilityLevel.NET_4_6;
                case ApiCompatibility.STANDARD_2_x: return ApiCompatibilityLevel.NET_Standard_2_0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(apiCompatibility), apiCompatibility, null);
            }

#endif
        }

        private ScriptingImplementation GetScriptingImplementation(ScriptingBackend scriptingBackend)
        {
            switch (scriptingBackend)
            {
                case ScriptingBackend.IL2CPP: return ScriptingImplementation.IL2CPP;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scriptingBackend), scriptingBackend, null);
            }
        }
    }
}