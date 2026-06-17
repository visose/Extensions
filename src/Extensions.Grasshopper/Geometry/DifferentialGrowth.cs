using Rhino.Geometry;
using DifferentialGrowthSimulation = Extensions.Simulations.DifferentialGrowth.DifferentialGrowth;

namespace Extensions.Grasshopper;

public class DifferentialGrowth() : Component(
    "Differential Growth",
    "DiffGrowth",
    "Grows polylines with a differential-growth simulation.",
    "Geometry",
    "{64C4B469-E923-4B7E-B746-C2599F7ED0A0}",
    "Virus")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddGeometryParameter("Region", "R", "Planar boundary curve or mesh surface.", GH_ParamAccess.item);
        _ = pManager.AddCurveParameter("Polylines", "P", "Seed polylines.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Radius", "R", "Collision radius.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Iterations", "I", "Maximum growth iterations.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Convergence", "C", "Relaxation iterations per growth step.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Polylines", "P", "Generated polylines by iteration.", GH_ParamAccess.tree);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var region = DA.Get<GeometryBase>(0);
        var inPolylines = DA.List<Curve>(1).Select(static curve => curve.ToPolyline()).ToList();
        Polyline? polyline = null;
        Mesh? mesh = null;

        if (region is Curve curve)
            polyline = curve.ToPolyline();
        else
            mesh = region as Mesh ?? throw new ArgumentException("Region should be a polyline or mesh.");

        DifferentialGrowthSimulation simulation = new(
            inPolylines,
            DA.Get<double>(2),
            DA.Get<int>(4),
            DA.Get<int>(3),
            polyline,
            mesh);

        DA.SetCurveTree(0, simulation.AllPolylines);
    }
}
