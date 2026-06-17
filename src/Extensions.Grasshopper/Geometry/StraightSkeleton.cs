using Rhino.Geometry;
using StraightSkeletonCore = Extensions.StraightSkeleton.StraightSkeleton;

namespace Extensions.Grasshopper;

public class StraightSkeleton() : Component(
    "Straight Skeleton",
    "StrSkel",
    "Computes the straight skeleton of a polygon.",
    "Geometry",
    "{d529efd9-2fdd-4751-a6b4-307c8f82390b}",
    "Graph")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Polygon", "P", "Closed polygon.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Regions", "R", "Straight skeleton regions.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var regions = StraightSkeletonCore.GetStraightSkeleton(DA.Get<Curve>(0).ToPolyline());
        DA.SetDataList(0, regions.Select(static region => region.ToNurbsCurve()));
    }
}
