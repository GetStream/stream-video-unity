using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        var versionLine = $"v{version.Major}.{version.Minor}.{version.Build}:";
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
        GetVersionProperty(packageJsonFilePath, out var json);

        json[PackageJsonVersionPropertyName] = $"{version.Major}.{version.Minor}.{version.Build}";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var updatedJsonContent = json.ToJsonString(options);

        File.WriteAllText(packageJsonFilePath, updatedJsonContent);

        Console.WriteLine($"Updated the package.json file in `{packageJsonFilePath}`");
    }

    //StreamTodo: move this to some PackageJsonParser -> ctor would accept the path. and Parse would return readonly PackageJson with properties like Version
    // and DisplayName that is used in samples. So we need to run git mv old_samples_path to git mv new_samples_path and samples path is Assets/Samples/DisplayName/Version  
    public JsonNode GetVersionProperty(string packageJsonFilePath, out JsonNode jsonFileNode)
    {
        var fileContents = File.ReadAllText(packageJsonFilePath);

        jsonFileNode = JsonNode.Parse(fileContents) ??
                       throw new InvalidOperationException($"Failed to parse the JSON file: `{packageJsonFilePath}`.");

        var versionProperty = jsonFileNode[PackageJsonVersionPropertyName];
        if (versionProperty == null)
        {
            throw new InvalidOperationException($"Failed to find the `{PackageJsonVersionPropertyName}` property in `{
                packageJsonFilePath}`");
        }

        return versionProperty;
    }

    private const string PackageJsonVersionPropertyName = "version";
}