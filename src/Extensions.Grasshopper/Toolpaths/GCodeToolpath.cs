using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using GCodeToolpathCore = Extensions.Toolpaths.Milling.GCodeToolpath;

namespace Extensions.Grasshopper;

public class GCodeToolpath() : RobotComponent(
    "G-code Toolpath",
    "GPath",
    "Creates a milling toolpath from a G-code file.",
    "Toolpaths",
    "{862CF2F9-EF08-444C-88B5-459D365EB60A}",
    "Fingerprint")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddTextParameter("File", "F", "G-code file path.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new TargetParameter(), "Target", "T", "Reference cartesian target.", GH_ParamAccess.item);
        _ = pManager.AddPointParameter("Point Alignment", "P", "Optional point used to align target X axes.", GH_ParamAccess.item);
        _ = pManager.AddBooleanParameter("Add Bit", "A", "Add end mill geometry to the tool.", GH_ParamAccess.item);

        pManager[2].Optional = true;
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Robot toolpath.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ToolParameter(), "Spindle", "S", "Spindle with end mill.", GH_ParamAccess.item);
        _ = pManager.AddPlaneParameter("MCS", "P", "Plane of machine coordinate system.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Rapid Indices", "R", "Target indices that delimit rapid sections.", GH_ParamAccess.list);
        _ = pManager.AddTextParameter("Ignored Lines", "I", "Ignored G-code lines.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var target = DA.Get<Target>(1) as CartesianTarget
            ?? throw new ArgumentException("Reference target must be a cartesian target.");

        var alignment = DA.MaybeValue<Point3d>(2) is { } point
            ? (Vector3d)point
            : Vector3d.XAxis;

        GCodeToolpathCore toolpath = new(DA.Get<string>(0), target, alignment, DA.Get<bool>(3));
        var (tool, mcs, rapidStarts, ignored) = toolpath;

        DA.SetData(0, toolpath);
        DA.SetData(1, tool);
        DA.SetData(2, mcs.Plane);
        DA.SetDataList(3, rapidStarts);
        DA.SetDataList(4, ignored);
    }
}
