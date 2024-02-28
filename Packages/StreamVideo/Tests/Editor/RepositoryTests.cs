using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    internal class RepositoryTests
    {
        [UnityTest]
        public IEnumerator Imported_samples_match_package_source_samples()
        {
            const string packageName = "io.getstream.video";
            var fileComparer = new SimpleFileCompare();

            var listPackagesRequest = Client.List();

            while (!listPackagesRequest.IsCompleted)
            {
                yield return null;
            }

            // Get unity package
            var packages = listPackagesRequest.Result;
            var streamVideoUnityPackage = packages.First(p => p.name == packageName);
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
                = Path.Combine(currentDirectory, "Assets\\Samples\\", displayName, version);

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
                    sb.AppendLine($"Sample imported files vs source files mismatch. Package: `{sampleDisplayName}`. Differences:");
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
        

    }
}