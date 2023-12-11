using System.Text;
using System.Text.RegularExpressions;

namespace ReleaseTool;

internal class FileWriter
{
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

            string newVersionString = $"new Version({version.Major}, {version.Minor}, {version.Build})";

            const string regexPattern = @"new Version\([\s]*([0-9]+)[\s]*,[\s]*([0-9]+)[\s]*,[\s]*([0-9]+)[s]*\)";
            var regex = new Regex(regexPattern);
            var replaced = regex.Replace(line, newVersionString);

            if (replaced == line)
            {
                throw new Exception($"Failed to regex parse `{versionFilePath}`");
            }

            sb.AppendLine(replaced);
        }
        
        File.WriteAllText(versionFilePath, sb.ToString());
        
        Console.WriteLine("Updated the version file");
    }

    public void WriteChangelogFile(Version version, string changelog, string changelogFilePath)
    {
        var sb = new StringBuilder();

        var versionLine = $"v{version.Major}.{version.Minor}.{version.Build}:";
        sb.AppendLine(versionLine);

        changelog = changelog.Trim();
        var newChangelogLines = changelog.Split("\n");

        var oldChangelogLines = File.ReadAllLines(changelogFilePath);

        foreach (var line in newChangelogLines)
        {
            sb.AppendLine(line);
        }

        foreach (var line in oldChangelogLines)
        {
            sb.AppendLine(line);
        }

        File.WriteAllText(changelogFilePath, sb.ToString());
        
        Console.WriteLine("Updated the changelog file");
    }
}