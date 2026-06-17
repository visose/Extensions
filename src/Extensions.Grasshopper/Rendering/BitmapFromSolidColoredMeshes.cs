using Rhino.Display;
using Rhino.Geometry;
using Extensions.Document;

namespace Extensions.Grasshopper;

public class BitmapFromSolidColoredMeshes() : Component(
    "Bitmap from Meshes",
    "BmpMeshes",
    "Creates a bitmap from solid-colored meshes and assigns matching UV coordinates.",
    "Rendering",
    "{db4d6bc7-9e3c-459b-8494-6fd92dc526ca}",
    "PaintBrush02")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Meshes", "M", "Solid-colored meshes.", GH_ParamAccess.list);
        _ = pManager.AddTextParameter("File Path", "F", "PNG file path.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new DisplayGeometryParameter(), "Display Geometry", "M", "Display geometry with bitmap texture.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var file = DA.Get<string>(1);
        var outMeshes = RenderExtensions.BitmapFromSolidColoredMeshes(DA.List<Mesh>(0), file);

        Mesh joinedMesh = new();

        foreach (var mesh in outMeshes)
            joinedMesh.Append(mesh);

        DisplayMaterial material = new();
        material.SetBitmapTexture(file, true);

        DA.SetData(0, new DisplayGeometry(joinedMesh, material));
    }
}
