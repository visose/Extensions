using RhinoPackager;
using RhinoPackager.Commands;

var app = App.Create(args);
Props props = new("Directory.Build.props");
Github github = new("visose", "Extensions");

app.Add(
    [
        new CheckVersion
        (
            props: props,
            github: github
        ),
        new Build
        (
            buildProject: "src/Extensions.Grasshopper/Extensions.Grasshopper.csproj"
        ),
        new Yak
        (
            props: props,
            sourceFolder: "artifacts/bin/Extensions.Grasshopper/net48",
            files:
            [
                "Extensions.gha",
                "Extensions.dll",
                "clipper_library.dll",
                "geometry3Sharp.dll",
                "gsGCode.dll",
                "MoreLinq.dll",
                "SkeletonNet.dll",
                "icon.png"
            ],
            tags:
            [
                "rh7_0-any",
                "rh8_0-any"
            ]
        ),
        new Release
        (
            props: props,
            github: github,
            notesFile: "RELEASE",
            message: "> This **release** can only be installed through the package manager in **Rhino 7** using the `_PackageManager` command.\n> Check the [readme](../../blob/master/.github/README.md) for more details."
        )
    ]);

await app.RunAsync();
