using RhinoPackager;
using RhinoPackager.Commands;

var app = App.Create(args);
var github = new Github("visose", "Extensions");

app.Add(new ICommand[]
    {
        new CheckVersion(github),
        new Build
        (
            buildProject: "src/Extensions.Grasshopper/Extensions.Grasshopper.csproj"
        ),
        new Yak
        (
            propsFile: "Directory.Build.props",
            sourceFolder: "artifacts/bin/Extensions.Grasshopper/net48",
            files: new []
            {
                "Extensions.gha",
                "Extensions.dll",
                "clipper_library.dll",
                "geometry3Sharp.dll",
                "gsGCode.dll",
                "MoreLinq.dll",
                "SkeletonNet.dll"
            },
            tag: "rh7_0-any"
        ),
        new Release
        (
            github: github,
            file: "RELEASE",
            message: "> This **release** can only be installed through the package manager in **Rhino 7** using the `_PackageManager` command.\n> Check the [readme](../../blob/master/.github/README.md) for more details."
        )
    });

await app.RunAsync();