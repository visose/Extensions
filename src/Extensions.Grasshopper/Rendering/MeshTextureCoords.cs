using Rhino.Geometry;

namespace Extensions.Grasshopper;

public class MeshTextureCoords() : Component(
    "Texture Coordinates",
    "TexCoords",
    "Sets mesh texture coordinates from points.",
    "Rendering",
    "{297d173d-4eac-4a93-947c-fa8216e73cfa}",
    "EyeDropper")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh to update.", GH_ParamAccess.item);
        _ = pManager.AddPointParameter("Texture Coordinates", "T", "Texture coordinates. Z values are ignored.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh with texture coordinates.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var outMesh = RenderExtensions.SetTextureCoords(DA.Get<Mesh>(0), DA.List<Point3d>(1));
        DA.SetData(0, outMesh);
    }
}
