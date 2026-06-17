using Rhino.Geometry;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;

namespace Extensions.Grasshopper;

public class CreateExternalExtrusionToolpath() : RobotComponent(
    "External Extrusion Toolpath",
    "ExtPath",
    "Creates an extrusion toolpath using an external axis.",
    "Toolpaths",
    "{08731061-8020-4204-8B69-198AC90BCE5E}",
    "LayersAdd")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Paths", "P", "Extrusion paths as polylines.", GH_ParamAccess.list);
        _ = pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion Attributes", "A", "Extrusion toolpath settings.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Extrusion Factor", "F", "External-axis extrusion scale.", GH_ParamAccess.item, 0.21);
        _ = pManager.AddNumberParameter("Suck Back", "Sb", "Reverse extrusion distance after stopping.", GH_ParamAccess.item, 0);
        _ = pManager.AddNumberParameter("Start Distance", "Sd", "Extrusion distance before robot motion starts.", GH_ParamAccess.item, 0);
        _ = pManager.AddNumberParameter("Test Loop", "L", "Test loop distance.", GH_ParamAccess.item, 200);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Robot toolpath.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Layer Indices", "L", "First target index of each layer.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var polylines = DA.List<Curve>(0).Select(static curve => curve.ToPolyline()).ToArray();
        ExternalExtrusionToolpath toolpath = new(
            polylines,
            DA.Get<ExtrusionAttributes>(1),
            DA.Get<double>(2),
            DA.Get<double>(3),
            DA.Get<double>(4),
            DA.Get<double>(5));

        DA.SetData(0, toolpath);
        DA.SetDataList(1, toolpath.SubPrograms);
    }
}
