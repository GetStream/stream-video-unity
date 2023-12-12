namespace ReleaseTool;

internal class Validator
{
    public Version ParseVersion(string version)
    {
        if (version.StartsWith("v"))
        {
            version = version[1..];;
        }

        return new Version(version);
    }

    public void AssertThatNewVersionGreaterThanCurrent(Version newVersion, Version currentVersion)
    {
        if (newVersion <= currentVersion)
        {
            throw new InvalidOperationException($"New version ({newVersion}) must be greater than current version ({
                currentVersion}).");
        }
    }
}