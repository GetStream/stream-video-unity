#if STREAM_TESTS_ENABLED
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StreamVideo.Tests.Editor;
using StreamVideo.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    internal class RepositoryTests
    {
        [UnityTest]
        public IEnumerator Imported_samples_match_package_source_samples()
            => Imported_samples_match_package_source_samples_Async().RunAsIEnumerator();

        public async Task Imported_samples_match_package_source_samples_Async()
        {
            var fileComparer = new SimpleFileCompare();

            var streamVideoUnityPackage = await TestUtils.GetStreamVideoPackageInfo();
            var packageSourcePath = streamVideoUnityPackage.resolvedPath;
            var displayName = streamVideoUnityPackage.displayName;
            var version = streamVideoUnityPackage.version;

            // Read samples from package.json directly because PackageInfo does not contain samples info
            var packageJsonPath = Path.Combine(packageSourcePath, "package.json");
            var packageJson = JObject.Parse(File.ReadAllText(packageJsonPath));
            var samples = packageJson["samples"];

            if (samples == null)
            {
                throw new InvalidOperationException("Failed to read samples from package");
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            var importedPackageSamplesParentPath
                = Path.Combine(currentDirectory, "Assets", "Samples", displayName, version);

            foreach (var sample in samples)
            {
                if (sample["displayName"] == null || sample["path"] == null)
                {
                    throw new InvalidOperationException("Failed to read package displayName or path");
                }

                var sampleDisplayName = sample["displayName"].ToString();
                var sampleSubPath = sample["path"].ToString();

                var importedSampleAssetsPath = Path.Combine(importedPackageSamplesParentPath, sampleDisplayName);
                var sourceSamplePath = Path.Combine(packageSourcePath, sampleSubPath);

                var importedSampleFileListing
                    = new DirectoryInfo(importedSampleAssetsPath).GetFiles("*.*", SearchOption.AllDirectories);
                var sourceSampleFileListing
                    = new DirectoryInfo(sourceSamplePath).GetFiles("*.*", SearchOption.AllDirectories);

                var isSequenceEqual = importedSampleFileListing.SequenceEqual(sourceSampleFileListing, fileComparer);
                if (!isSequenceEqual)
                {
                    var diff1 = importedSampleFileListing.Except(sourceSampleFileListing, fileComparer);
                    var diff2 = sourceSampleFileListing.Except(importedSampleFileListing, fileComparer);

                    var sb = new StringBuilder();
                    sb.AppendLine(
                        $"Sample imported files vs source files mismatch. Package: `{sampleDisplayName}`. Differences:");
                    sb.AppendLine("Imported vs Source Diff in files: " + diff1.Count());
                    foreach (var diff in diff1)
                    {
                        sb.AppendLine("- " + diff.Name);
                    }

                    sb.AppendLine("Source vs Imported Diff in files: " + diff2.Count());
                    foreach (var diff in diff2)
                    {
                        sb.AppendLine("- " + diff.Name);
                    }

                    Debug.Log(sb.ToString());
                }

                Assert.IsTrue(isSequenceEqual);
            }
        }

        [UnityTest]
        public IEnumerator Dtos_do_not_contain_json_required_always_flag()
            => Dtos_do_not_contain_json_required_always_flag_Async().RunAsIEnumerator();

        private async Task Dtos_do_not_contain_json_required_always_flag_Async()
        {
            var streamVideoUnityPackage = await TestUtils.GetStreamVideoPackageInfo();
            var packageSourcePath = streamVideoUnityPackage.resolvedPath;

            var internalDtosDirectory = Path.Combine(packageSourcePath, "Runtime", "Core", "InternalDTO");

            const string internalDtoNamespace = "namespace StreamVideo.Core.InternalDTO";
            const string sfuNamespace = "namespace StreamVideo.v1.Sfu";
            var sfuNamespaces = new string[] { "namespace StreamVideo.v1.Sfu", "StreamVideo.Core.Sfu" };
            const string jsonAlwaysRequiredFlag = "Newtonsoft.Json.Required.Always";

            var files = Directory.GetFiles(internalDtosDirectory, "*.cs", SearchOption.AllDirectories);

            var sb = new StringBuilder();
            sb.AppendLine("These files contain invalid json flag:");
            var anyFailed = false;
            foreach (var file in files)
            {
                var content = await File.ReadAllTextAsync(file);

                var isDtoNamespace = content.Contains(internalDtoNamespace);
                var isSfuNamespace = sfuNamespaces.Any(content.Contains);
                Assert.IsTrue(isDtoNamespace || isSfuNamespace);

                if (isSfuNamespace)
                {
                    continue;
                }

                if (content.Contains(jsonAlwaysRequiredFlag))
                {
                    anyFailed = true;
                    sb.AppendLine(
                        $"- File: `{file}` contains `{jsonAlwaysRequiredFlag}` flag. This should be changed to `Newtonsoft.Json.Required.Default`. Otherwise, the deserialization is too strict and may fail after API changes.");
                }
            }

            if (anyFailed)
            {
                Debug.Log(sb.ToString());
            }

            Assert.IsFalse(anyFailed);
        }
    }
}
#endif