using System.Text.Json.Serialization;

namespace ReleaseTool.PackageManifest;

public class PackageSample
{
    [JsonPropertyName("displayName")] 
    public string DisplayName { get; set; } = default!;
}