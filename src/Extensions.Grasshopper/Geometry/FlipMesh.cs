using Rhino.Geometry;

namespace Extensions.Grasshopper;

public class FlipMesh() : Component(
    "Flip Mesh",
    "MeshFlip",
    "Flips mesh face orientation.",
    "Geometry",
    "{65433478-f2d7-4cd0-808f-b1d1834270c3}",
    "Undo")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh to flip.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Flipped mesh.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var outMesh = DA.Get<Mesh>(0).DuplicateMesh();
        outMesh.Flip(true, true, true);

        DA.SetData(0, outMesh);
    }
}
