using ReleaseTool;
using ReleaseTool.PackageManifest;

var parser = new ArgsParser();
parser.Parse(args, out var version, out var changelog);

var packageParser = new PackageParser();
var builder = new ReleaseBuilder(new Validator(), new FileFinder(), new FileWriter(packageParser), packageParser);
builder.Execute(version, changelog);