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
        var newVersion = _validator.ParseVersion(version);

        _fileFinder.FindReleaseFiles(out var versionFilePath, out var changelogFilePath);

        Console.WriteLine("Version filepath: " + versionFilePath);
        Console.WriteLine("Changelog filepath: " + changelogFilePath);

        // Todo: Verify that new version is greater than the current

        _fileWriter.WriteChangelogFile(newVersion, changelog, changelogFilePath);
        _fileWriter.WriteVersionFile(newVersion, versionFilePath);
    }

    private readonly FileFinder _fileFinder;
    private readonly FileWriter _fileWriter;
    private readonly Validator _validator;
}