using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReleaseTool.PackageManifest;

public class PackageParser
{
    public const string PackageJsonVersionPropertyName = "version";

    public PackageInfo ParsePackageJson(string packageJsonFilePath)
    {
        var fileContents = File.ReadAllText(packageJsonFilePath);
        return JsonSerializer.Deserialize<PackageInfo>(fileContents);
    }

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
}