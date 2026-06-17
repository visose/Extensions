using RhinoPackager;
using RhinoPackager.Commands;

var app = App.Create(args);
Props props = new("src/Package.props");
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
            project: "src/Extensions.Grasshopper/Extensions.Grasshopper.csproj",
            target: "build"
        ),
        new Yak
        (
            props: props,
            sourceFolder: "artifacts/bin/Extensions.Grasshopper/release",
            tags:
            [
                "rh8_21-any"
            ],
            exclude:
            [
                "*.pdb",
                "*.deps.json"
            ]
        ),
        new Release
        (
            props: props,
            github: github,
            notesFile: "RELEASE",
            message: "> This **release** can only be installed through the package manager in **Rhino 8** using the `_PackageManager` command.\n> Check the [readme](../../blob/master/.github/README.md) for more details."
        )
    ]);

return await app.Run();
