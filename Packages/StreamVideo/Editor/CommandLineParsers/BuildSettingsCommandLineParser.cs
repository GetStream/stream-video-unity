using System;
using System.Collections.Generic;
using StreamVideo.EditorTools.Builders;
using UnityEditor;
using UnityEngine;

namespace StreamVideo.EditorTools.CommandLineParsers
{
    public class BuildSettingsCommandLineParser : CommandLineParserBase<BuildSettings>
    {
        public const string ApiCompatibilityArgKey = "-apiCompatibility";
        public const string ScriptingBackendArgKey = "-scriptingBackend";
        public const string BuildTargetPlatformArgKey = "-buildTargetPlatform";
        public const string BuildTargetPathArgKey = "-buildTargetPath";

        protected override BuildSettings Parse(IDictionary<string, string> args)
        {
            if (IsAnyRequiredArgMissing(args, out var missingArgsInfo, BuildTargetPlatformArgKey,
                    ApiCompatibilityArgKey, ScriptingBackendArgKey, BuildTargetPathArgKey))
            {
                throw new ArgumentException($"Missing argument: `{missingArgsInfo}`");
            }

            if (!TryParseEnum<BuildTargetPlatform>(args[BuildTargetPlatformArgKey], ignoreCase: true,
                    out var targetPlatform))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[BuildTargetPlatformArgKey]}` to enum: {typeof(BuildTargetPlatform)}");
            }

            if (!TryParseEnum<ApiCompatibility>(args[ApiCompatibilityArgKey], ignoreCase: true,
                    out var apiCompatibility))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[ApiCompatibilityArgKey]}` to enum: {typeof(BuildTargetPlatform)}");
            }

            if (!TryParseEnum<ScriptingBackend>(args[ScriptingBackendArgKey], ignoreCase: true,
                    out var scriptingBackend))
            {
                throw new ArgumentException(
                    $"Failed to parse argument: `{args[ScriptingBackendArgKey]}` to enum: {typeof(BuildTargetPlatform)}");
            }

            var buildTargetGroup = GetBuildTargetGroup(targetPlatform);
            var apiCompatibilityLevel = GetApiCompatibilityLevel(apiCompatibility);
            var scriptingImplementation = GetScriptingImplementation(scriptingBackend);
            var targetPath = args[BuildTargetPathArgKey];

            return new BuildSettings(buildTargetGroup, apiCompatibilityLevel, scriptingImplementation, targetPath);
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case BuildTargetPlatform.Standalone: return BuildTargetGroup.Standalone;
                case BuildTargetPlatform.Mobile:
#if UNITY_STANDALONE_OSX
                    return BuildTargetGroup.iOS;
#else
                    return BuildTargetGroup.Android;
#endif
                case BuildTargetPlatform.Android: return BuildTargetGroup.Android;
                case BuildTargetPlatform.IOS: return BuildTargetGroup.iOS;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetPlatform), targetPlatform, null);
            }
        }

        private static ApiCompatibilityLevel GetApiCompatibilityLevel(ApiCompatibility apiCompatibility)
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

        private static ScriptingImplementation GetScriptingImplementation(ScriptingBackend scriptingBackend)
        {
            switch (scriptingBackend)
            {
                case ScriptingBackend.IL2CPP: return ScriptingImplementation.IL2CPP;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scriptingBackend), scriptingBackend, null);
            }
        }
        
        private static bool TryParseEnum<TEnum>(string rawValue, bool ignoreCase, out TEnum value) where TEnum : struct
        {
            Debug.Log($"Try parse `{rawValue}` to enum of type `{typeof(TEnum)}`");
            return Enum.TryParse(rawValue, ignoreCase, out value);
        }
    }
}
