using Rhino.Display;
using Rhino.Geometry;
using Extensions.Document;

namespace Extensions.Grasshopper;

public class BitMapFromVertexColors() : Component(
    "Bitmap from Mesh",
    "BmpMesh",
    "Creates a bitmap from mesh vertex colors and assigns matching UV coordinates.",
    "Rendering",
    "{7aedf2f4-75e2-48be-94c5-fe116caf8b26}",
    "PaintBrush01")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh with vertex colors.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("File Path", "F", "PNG file path.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new DisplayGeometryParameter(), "Display Geometry", "M", "Display geometry with bitmap texture.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var file = DA.Get<string>(1);
        var outMesh = RenderExtensions.BitmapFromVertexColors(DA.Get<Mesh>(0), file);
        DisplayMaterial material = new();
        material.SetBitmapTexture(file, true);

        DA.SetData(0, new DisplayGeometry(outMesh, material));
    }
}
