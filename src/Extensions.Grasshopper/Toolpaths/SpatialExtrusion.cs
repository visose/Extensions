using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.SpatialExtrusion;

namespace Extensions.Grasshopper;

public class CreateSpatialExtrusion() : RobotComponent(
    "Spatial Extrusion",
    "Spatial",
    "Creates a spatial extrusion toolpath from polylines.",
    "Toolpaths",
    "{79EE65B3-CB2F-4704-8B01-C7C9F379B7C4}",
    "Spatial")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Polylines", "P", "Extrusion polylines.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Variables", "V", "0. Extrusion diameter (mm).\r\n1. Plunge distance (mm).\r\n2. Unsupported nodes vertical offset (mm).\r\n3. Unsupported segments rotation compensation (rad).\r\n4. Distance ahead to stop in upwards segments as a factor of length (0..1).\r\n5. Horizontal displacement before downward segment (mm).", GH_ParamAccess.list);
        _ = pManager.AddParameter(new TargetParameter(), "Target", "T", "Reference cartesian target.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Speeds", "S", "0. Approach speed.\r\n1. Plunge speed.\r\n2. Supported segments.\r\n3. Downward segments.\r\n4. Unsupported nodes.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Wait Times", "W", "0. Wait time after extrusion.\r\n1. Wait time ahead stop.\r\n2. Wait on supported node before unsupported segment.\r\n3. Wait on unsupported node.", GH_ParamAccess.list);
        _ = pManager.AddIntegerParameter("Digital Outputs", "D", "Digital output indices connected to the extruder.", GH_ParamAccess.list);
        _ = pManager.AddGeometryParameter("Environment", "E", "Support geometry. Only meshes and polylines are supported.", GH_ParamAccess.list);

        pManager[6].Optional = true;
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new TargetParameter(), "Targets", "T", "Robot targets.", GH_ParamAccess.list);
        _ = pManager.AddLineParameter("Segments", "S", "Preview segments.", GH_ParamAccess.list);
        _ = pManager.AddIntegerParameter("Attributes", "A", "Preview segment attributes.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var polylines = DA.List<Curve>(0).Select(static curve => curve.ToPolyline()).ToArray();
        var target = DA.Get<Target>(2) as CartesianTarget
            ?? throw new ArgumentException("Target must be a cartesian target.");

        SpatialAttributes attributes = new(
            DA.List<double>(1),
            target,
            DA.List<double>(3),
            DA.List<double>(4),
            DA.List<int>(5),
            DA.MaybeList<GeometryBase>(6));
        SpatialExtrusion spatial = new(polylines, attributes);

        DA.SetDataList(0, spatial.Targets);
        DA.SetDataList(1, spatial.Display.Select(static d => d.segment));
        DA.SetDataList(2, spatial.Display.Select(static d => d.type));
    }
}
