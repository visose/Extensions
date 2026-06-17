using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Extensions.Grasshopper;

public class CurveSnap() : Component(
    "Curve Snap",
    "CrvSnap",
    "Snaps a curve to discrete segment lengths and directions.",
    "Discrete",
    "{4F45F86C-6B7E-4327-9475-467CB82DAF13}",
    "Polyline")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Curve", "C", "Curve to snap.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Segment Length", "L", "Target segment length.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Snap Type", "S", "Direction snapping method.", GH_ParamAccess.item, 0);
        _ = pManager.AddIntegerParameter("Subdivisions", "D", "Number of spherical subdivisions.", GH_ParamAccess.item, 0);

        var param = (Param_Integer)pManager[2];
        param.AddNamedValue("Equirectangular", 0);
        param.AddNamedValue("Icosahedral", 1);
        param.AddNamedValue("Quadrangular", 2);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Curve", "C", "Snapped curve.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var result = Discrete.CurveSnap.SnapCurve(
            DA.Get<Curve>(0),
            DA.Get<double>(1),
            (Discrete.CurveSnap.SnapType)DA.Get<int>(2),
            DA.Get<int>(3));

        DA.SetData(0, result);
    }
}
