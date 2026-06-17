using Rhino.Geometry;
using Extensions.Geometry;

namespace Extensions.Grasshopper;

public class RemesherComponent() : Component(
    "Remesher",
    "Remesher",
    "Remeshes a mesh with more even triangle edge lengths.",
    "Geometry",
    "{55D4CA7D-D9C7-485A-BC7A-BFDA387D4163}",
    "Triangle")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh to remesh.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Length", "L", "Target edge length.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Iterations", "I", "Remeshing iterations.", GH_ParamAccess.item, 50);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Remeshed mesh.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var outMesh = Remesh.Create(DA.Get<Mesh>(0), DA.Get<double>(1), DA.Get<int>(2));
        DA.SetData(0, outMesh);
    }
}
