namespace ReleaseTool;

internal class Validator
{
    public Version ParseVersion(string version)
    {
        if (!version.StartsWith("v"))
        {
            throw new ArgumentException("Version must start with `v`");
        }

        var trimmedVersion = version[1..];
        return new Version(trimmedVersion);
    }
}