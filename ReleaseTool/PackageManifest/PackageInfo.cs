using System.Text.Json.Serialization;

namespace ReleaseTool.PackageManifest;

public class PackageInfo
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = default!;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = default!;

    [JsonPropertyName("samples")] 
    public PackageSample[] Samples { get; set; } = default!;
}