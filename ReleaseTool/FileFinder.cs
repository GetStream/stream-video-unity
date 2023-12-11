namespace ReleaseTool;

internal class FileFinder
{
    public void FindReleaseFiles(out string versionFilePath, out string changelogFilePath)
    {
        var rootDirectory = FindSdkRootDirectory();
        changelogFilePath = FindChangelogFilePath(rootDirectory);
        versionFilePath = FindVersionFilePath(rootDirectory);
    }

    public string FindChangelogFilePath(string sdkRootDirectory)
    {
        // Changelog should be located in the sdk root directory
        var files = Directory.GetFiles(sdkRootDirectory);
        var changelogFilePath = files.FirstOrDefault(fullPath => Path.GetFileName(fullPath) == ChangelogFilename);
        if (changelogFilePath == null)
        {
            throw new FileNotFoundException($"Failed to find the changelog file in the sdk root directory: `{
                sdkRootDirectory}`. Searched for `{ChangelogFilename}`");
        }

        return changelogFilePath;
    }

    public string FindVersionFilePath(string sdkRootDirectory)
    {
        // Look in the "Packages" dir only so we don't search large folders like "Library"
        var packagesDir = GetPackagesDirectoryPath(sdkRootDirectory);
        
        var files = Directory.EnumerateFiles(packagesDir.FullName, $"*{SdkVersionFilename}", SearchOption.AllDirectories).ToArray();
        if (!files.Any())
        {
            throw new FileNotFoundException($"Failed to find the SDK version file in the packages directory: `{
                packagesDir.FullName}`. Searched for `{SdkVersionFilename}`");
        }

        return files.First();
    }



    public string FindPackageJsonFilePath(string sdkRootDirectory)
    {
        
    }

    /// <summary>
    /// Find SDK root directory - this is where the "Assets" directory is located
    /// </summary>
    /// <exception cref="Exception">Throws an exception when fails to find the root directory</exception>
    public string FindSdkRootDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        // Traverse upwards until we're at the same level as "Assets" directory
        while (Directory.GetDirectories(currentDirectory).All(dir => new DirectoryInfo(dir).Name != AssetsDirName))
        {
            var parentDirectory = Directory.GetParent(currentDirectory);
            if (parentDirectory == null)
            {
                throw new DirectoryNotFoundException($"Failed to find the `{AssetsDirName}` directory");
            }

            currentDirectory = parentDirectory.FullName;
        }

        return currentDirectory;
    }

    private const string PackagesDirName = "Packages";
    private const string AssetsDirName = "Assets";
    private const string SdkVersionFilename = "SdkVersionWrapper.cs";
    private const string ChangelogFilename = "CHANGELOG.md";
    private const string PackageJsonFilename = "package.json";
    
    private DirectoryInfo GetPackagesDirectoryPath(string sdkRootDirectory)
    {
        var packagesDir = new DirectoryInfo(Path.Combine(sdkRootDirectory, PackagesDirName));
        if (!packagesDir.Exists)
        {
            throw new DirectoryNotFoundException($"Failed to find the `Packages` dir: `{packagesDir}`");
        }

        return packagesDir;
    }
}