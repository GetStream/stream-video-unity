using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReleaseTool.PackageManifest;

namespace ReleaseTool;

internal class FileWriter
{
    public FileWriter(PackageParser packageParser)
    {
        _packageParser = packageParser;
    }

    public void WriteVersionFile(Version version, string versionFilePath)
    {
        var sb = new StringBuilder();
        var allLines = File.ReadAllLines(versionFilePath);
        foreach (var line in allLines)
        {
            if (!line.Contains("new Version"))
            {
                sb.AppendLine(line);
                continue;
            }

            var newVersionString = $"new Version({version.Major}, {version.Minor}, {version.Build})";

            const string regexPattern = @"new Version\([\s]*([0-9]+)[\s]*,[\s]*([0-9]+)[\s]*,[\s]*([0-9]+)[s]*\)";
            var regex = new Regex(regexPattern);
            var replaced = regex.Replace(line, newVersionString);

            if (replaced == line)
            {
                throw new Exception($"Failed to regex replace `{versionFilePath}`");
            }

            sb.AppendLine(replaced);
        }

        File.WriteAllText(versionFilePath, sb.ToString());

        Console.WriteLine($"Updated the version file in {versionFilePath}");
    }

    public void WriteChangelogFile(Version version, string changelog, string changelogFilePath)
    {
        var sb = new StringBuilder();

        var versionLine = $"{version.Major}.{version.Minor}.{version.Build}:";
        sb.AppendLine(versionLine);
        sb.AppendLine();

        changelog = changelog.Trim();
        var newChangelogLines = changelog.Split("\n");

        var oldChangelogLines = File.ReadAllLines(changelogFilePath);

        foreach (var line in newChangelogLines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();

        foreach (var line in oldChangelogLines)
        {
            sb.AppendLine(line);
        }

        File.WriteAllText(changelogFilePath, sb.ToString());

        Console.WriteLine($"Updated the changelog file in `{changelogFilePath}`");
    }

    public void WritePackageJsonFile(Version version, string packageJsonFilePath)
    {
        _packageParser.GetVersionProperty(packageJsonFilePath, out var json);

        json[PackageParser.PackageJsonVersionPropertyName] = $"{version.Major}.{version.Minor}.{version.Build}";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var updatedJsonContent = json.ToJsonString(options);

        File.WriteAllText(packageJsonFilePath, updatedJsonContent);

        Console.WriteLine($"Updated the package.json file in `{packageJsonFilePath}`");
    }

    public void MoveImportedSamples(PackageInfo packageInfo, ReleaseFilesInfo filesInfo, Version currentVersion,
        Version newVersion)
    {
        var currentDirPath = Path.Combine(filesInfo.AssetsSamplesDirectory, packageInfo.DisplayName,
            currentVersion.ToString());

        var currentDirMetaPath = $"{currentDirPath}.meta";

        var newDirPath = Path.Combine(filesInfo.AssetsSamplesDirectory, packageInfo.DisplayName,
            newVersion.ToString());
            
        var newDirMetaPath = $"{newDirPath}.meta";
        
        RunCommand("git", $"mv \"{currentDirPath}\" \"{newDirPath}\"");
        RunCommand("git", $"mv \"{currentDirMetaPath}\" \"{newDirMetaPath}\"");
    }

    private readonly PackageParser _packageParser;
    
    private void RunCommand(string command, string args)
    {
        Console.WriteLine($"Run command: {command} {args}");
        Process.Start(command, args);
    }
}