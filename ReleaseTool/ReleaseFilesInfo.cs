namespace ReleaseTool;

internal readonly struct ReleaseFilesInfo
{
    public readonly string VersionFilePath;
    public readonly string ChangelogFilePath;
    public readonly string PackageJsonFilePath;
    public readonly string AssetsSamplesDirectory;

    public ReleaseFilesInfo(string versionFilePath, string changelogFilePath, string packageJsonFilePath, string assetsSamplesDirectory)
    {
        VersionFilePath = versionFilePath;
        ChangelogFilePath = changelogFilePath;
        PackageJsonFilePath = packageJsonFilePath;
        AssetsSamplesDirectory = assetsSamplesDirectory;
    }
}