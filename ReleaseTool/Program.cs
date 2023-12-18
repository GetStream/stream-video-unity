using ReleaseTool;

var parser = new ArgsParser();
parser.Parse(args, out var version, out var changelog);

var builder = new ReleaseBuilder(new Validator(), new FileFinder(), new FileWriter());
builder.Execute(version, changelog);