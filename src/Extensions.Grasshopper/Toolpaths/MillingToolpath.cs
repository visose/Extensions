using Rhino.Geometry;
using Robots.Grasshopper;
using Extensions.Toolpaths.Milling;

namespace Extensions.Grasshopper;

public class CreateMillingToolpath() : RobotComponent(
    "Milling Toolpath",
    "MillPath",
    "Creates a milling toolpath from polylines.",
    "Toolpaths",
    "{49D4E0A2-DD27-4EA4-A887-5E9AF27ECF45}",
    "LayersSubtract")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Paths", "P", "Milling paths as polylines.", GH_ParamAccess.list);
        _ = pManager.AddBoxParameter("Bounding Box", "B", "Stock bounding box.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new MillingAttributesParameter(), "Milling Attributes", "A", "Milling toolpath settings.", GH_ParamAccess.item);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Robot toolpath.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Layer Indices", "L", "First target index of each layer.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var polylines = DA.List<Curve>(0).Select(static curve => curve.ToPolyline()).ToArray();
        MillingToolpath toolpath = new(polylines, DA.Get<Box>(1).BoundingBox, DA.Get<MillingAttributes>(2));

        DA.SetData(0, toolpath);
        DA.SetDataList(1, toolpath.SubPrograms);
    }
}
