using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;

namespace Extensions.Grasshopper;

public class CurveSnap : GH_Component
{
    public CurveSnap() : base("Curve Snap", "CrvSnap", "Snaps curves to discrete intervals and directions.", "Extensions", "Discrete") { }
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("Polyline");
    public override Guid ComponentGuid => new Guid("{4F45F86C-6B7E-4327-9475-467CB82DAF13}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curve", "C", "Curve.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Segment Length", "L", "Length of the segments.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Snap Type", "S", "Directions based on spherical mapping. Right click for types.", GH_ParamAccess.item, 0);
        pManager.AddIntegerParameter("Subdivisions", "D", "Number of spherical subdivisions.", GH_ParamAccess.item, 0);

        var param = pManager[2] as Param_Integer;
        param.AddNamedValue("Equirectangular", 0);
        param.AddNamedValue("Icosahedral", 1);
        param.AddNamedValue("Quadrangular", 2);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Discretized Curve", "C", "Discretized curve.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        Curve curve = null;
        double length = 0;
        int snapType = 0;
        int divisions = 0;

        if (!DA.GetData(0, ref curve)) return;
        if (!DA.GetData(1, ref length)) return;
        if (!DA.GetData(2, ref snapType)) return;
        if (!DA.GetData(3, ref divisions)) return;

        var result = Discrete.CurveSnap.SnapCurve(curve, length, (Discrete.CurveSnap.SnapType)snapType, divisions);
        DA.SetData(0, result);
    }
}
