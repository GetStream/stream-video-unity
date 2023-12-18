namespace ReleaseTool;

internal class ReleaseBuilder
{
    public ReleaseBuilder(Validator validator, FileFinder fileFinder, FileWriter fileWriter)
    {
        _validator = validator;
        _fileFinder = fileFinder;
        _fileWriter = fileWriter;
    }

    /// <summary>
    /// Apply repository changes to reflect the new version
    /// </summary>
    /// <param name="version">New version to be released</param>
    /// <param name="changelog">Changelog section related to the new version</param>
    public void Execute(string version, string changelog)
    {
        var filesInfo = _fileFinder.FindReleaseFiles();
        
        var newVersion = _validator.ParseVersion(version);
        var currentVersionProp = _fileWriter.GetVersionProperty(filesInfo.PackageJsonFilePath, out _);
        var currentVersion = _validator.ParseVersion(currentVersionProp.GetValue<string>());
        
        _validator.AssertThatNewVersionGreaterThanCurrent(newVersion, currentVersion);

        Console.WriteLine("Version filepath: " + filesInfo.VersionFilePath);
        Console.WriteLine("Changelog filepath: " + filesInfo.ChangelogFilePath);
        Console.WriteLine("Package.json filepath: " + filesInfo.PackageJsonFilePath);

        _fileWriter.WriteChangelogFile(newVersion, changelog, filesInfo.ChangelogFilePath);
        _fileWriter.WriteVersionFile(newVersion, filesInfo.VersionFilePath);
        _fileWriter.WritePackageJsonFile(newVersion, filesInfo.PackageJsonFilePath);
    }

    private readonly FileFinder _fileFinder;
    private readonly FileWriter _fileWriter;
    private readonly Validator _validator;
}