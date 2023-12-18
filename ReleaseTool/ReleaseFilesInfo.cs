namespace ReleaseTool;

internal readonly struct ReleaseFilesInfo
{
    public readonly string VersionFilePath;
    public readonly string ChangelogFilePath;
    public readonly string PackageJsonFilePath;

    public ReleaseFilesInfo(string versionFilePath, string changelogFilePath, string packageJsonFilePath)
    {
        VersionFilePath = versionFilePath;
        ChangelogFilePath = changelogFilePath;
        PackageJsonFilePath = packageJsonFilePath;
    }
}