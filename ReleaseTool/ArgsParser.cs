using  System.CommandLine;
using System.Text;

namespace ReleaseTool;

internal sealed class ArgsParser
{
    public void Parse(string[] args, out string version, out string changelog)
    {
        var versionArg = new Option<string>(
            name: "--version",
            description: "The new version that will be set")
        {
            IsRequired = true
        };

        var changelogBase64Arg = new Option<string>(
            name: "--changelogBase64",
            description: "Changelog section related to the new version")
        {
            IsRequired = true
        };

        var rootCommand = new RootCommand("Release Tool");
        rootCommand.AddOption(versionArg);
        rootCommand.AddOption(changelogBase64Arg);

        var result = rootCommand.Parse(args);
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to parse arguments. Arguments: `{versionArg.Name}` and `{changelogBase64Arg.Name}` are required.");
        }

        version = result.GetValueForOption(versionArg) ?? throw new InvalidOperationException($"Failed to get `{versionArg.Name}` argument");
        var changelogBase64 = result.GetValueForOption(changelogBase64Arg) ?? throw new InvalidOperationException($"Failed to get `{changelogBase64Arg.Name}` argument");
        changelog = DecodeBase64(changelogBase64);
    }
    
    private static string DecodeBase64(string value)
    {
        var valueBytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(valueBytes);
    }
}