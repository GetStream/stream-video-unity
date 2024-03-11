namespace ReleaseTool;

internal class FileFinder
{
    public ReleaseFilesInfo FindReleaseFiles()
    {
        var rootDirectory = FindSdkRootDirectory();
        var changelogFilePath = FindChangelogFilePath(rootDirectory);
        var versionFilePath = FindVersionFilePath(rootDirectory);
        var packageJsonFilePath = FindPackageJsonFilePath(rootDirectory);
        var assetsSamplesDirPath = FindAssetsSamplesDirPath(rootDirectory);

        return new ReleaseFilesInfo(versionFilePath, changelogFilePath, packageJsonFilePath, assetsSamplesDirPath);
    }

    private const string PackagesDirName = "Packages";
    private const string AssetsDirName = "Assets";
    private const string SdkVersionFilename = "SdkVersionWrapper.cs";
    private const string ChangelogFilename = "CHANGELOG.md";
    private const string PackageJsonFilename = "package.json";
    
    private string FindChangelogFilePath(string sdkRootDirectory) => FindFile(sdkRootDirectory, ChangelogFilename, SearchOption.TopDirectoryOnly);

    private string FindVersionFilePath(string sdkRootDirectory)
    {
        // Look in the "Packages" dir only so we don't search large folders like "Library"
        var packagesDir = GetPackagesDirectoryPath(sdkRootDirectory);
        
        return FindFile(packagesDir.FullName, SdkVersionFilename, SearchOption.AllDirectories);
    }

    private string FindPackageJsonFilePath(string sdkRootDirectory)
    {
        var packagesDir = GetPackagesDirectoryPath(sdkRootDirectory);

        return FindFile(packagesDir.FullName, PackageJsonFilename, SearchOption.AllDirectories);
    }

    private string FindAssetsSamplesDirPath(string sdkRootDirectory) => Path.Combine(sdkRootDirectory, "Samples");

    /// <summary>
    /// Find SDK root directory - this is where the "Assets" directory is located
    /// </summary>
    /// <exception cref="Exception">Throws an exception when fails to find the root directory</exception>
    private string FindSdkRootDirectory()
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
    
    private static DirectoryInfo GetPackagesDirectoryPath(string sdkRootDirectory)
    {
        var packagesDir = new DirectoryInfo(Path.Combine(sdkRootDirectory, PackagesDirName));
        if (!packagesDir.Exists)
        {
            throw new DirectoryNotFoundException($"Failed to find the `{PackagesDirName}` dir: `{packagesDir}`");
        }

        return packagesDir;
    }
    
    private static string FindFile(string searchRootPath, string searchFilename, SearchOption searchOption)
    {
        var files = Directory.EnumerateFiles(searchRootPath, $"*{searchFilename}", searchOption).ToArray();
        if (!files.Any())
        {
            throw new FileNotFoundException($"Failed to find `{searchFilename}` file in `{searchRootPath}`");
        }

        if (files.Length > 1)
        {
            throw new FileNotFoundException($"Found multiple `{searchFilename}` files in `{searchRootPath}`. Expected one. Results: " + string.Join(", ", files));
        }

        return files.First();
    }
}