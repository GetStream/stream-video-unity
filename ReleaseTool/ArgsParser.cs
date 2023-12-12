using  System.CommandLine;

namespace ReleaseTool;

internal sealed class ArgsParser
{
    public void Parse(string[] args, out string version, out string changelog)
    {
        var versionArg = new Option<string>(
            name: "--version",
            description: "The new version that will be set");

        var changelogArg = new Option<string>(
            name: "--changelog",
            description: "Changelog section related to the new version");
        
        var rootCommand = new RootCommand("Release Tool");
        rootCommand.AddOption(versionArg);
        rootCommand.AddOption(changelogArg);

        var result = rootCommand.Parse(args);
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to parse arguments. Arguments: `{versionArg.Name}` and `{changelogArg.Name}` are required.");
        }

        version = result.GetValueForOption(versionArg) ?? throw new InvalidOperationException($"Failed to get `{versionArg.Name}` argument");
        changelog = result.GetValueForOption(changelogArg) ?? throw new InvalidOperationException($"Failed to get `{changelogArg.Name}` argument");
    }
}