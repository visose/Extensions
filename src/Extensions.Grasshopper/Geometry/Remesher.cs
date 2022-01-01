using Grasshopper.Kernel;
using Rhino.Geometry;
using Extensions.Geometry;

namespace Extensions.Grasshopper;

public class RemesherComponent : GH_Component
{
    public RemesherComponent() : base("Remesher", "Remesher", "Triangular remeshing trying to keep edge lengths as equal as possible.", "Extensions", "Geometry") { }
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("Triangle");
    public override Guid ComponentGuid => new Guid("{55D4CA7D-D9C7-485A-BC7A-BFDA387D4163}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Input mesh.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Length", "L", "Target edge length.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Iterations", "I", "Number of iterations.", GH_ParamAccess.item, 50);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Resulting mesh.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        Mesh mesh = new Mesh();
        double length = 0.0;
        int iterations = 50;
        DA.GetData(0, ref mesh);
        DA.GetData(1, ref length);
        DA.GetData(2, ref iterations);

        Mesh outMesh = Remesh.RemeshTest(mesh, length, iterations);
        DA.SetData(0, outMesh);
    }
}
