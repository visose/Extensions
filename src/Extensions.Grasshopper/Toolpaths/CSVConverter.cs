using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using CSVConverterCore = Extensions.Toolpaths.CSVConverter;

namespace Extensions.Grasshopper;

public class CSVConverter() : RobotComponent(
    "CSV Toolpath",
    "CSVPath",
    "Creates a robot toolpath from a CSV file.",
    "Toolpaths",
    "{0C7F5A9E-40CC-4A87-AB23-6333D274FF14}",
    "Fingerprint")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddTextParameter("File", "F", "CSV file path.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new TargetParameter(), "Target", "T", "Reference cartesian target.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("Mask", "M", "CSV column mask. Allowed values are position, normal, xaxis, speed, zone, and type.", GH_ParamAccess.item, "position, normal, speed");
        _ = pManager.AddBooleanParameter("Reverse Normal", "R", "Reverse target normals.", GH_ParamAccess.item, false);
        _ = pManager.AddNumberParameter("Cut Speed", "C", "Maximum speed in mm/min for cutting moves.", GH_ParamAccess.item, 2750);
        _ = pManager.AddPointParameter("Point Alignment", "P", "Optional point used to align target X axes.", GH_ParamAccess.item);

        pManager[4].Optional = true;
        pManager[5].Optional = true;
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new TargetParameter(), "Targets", "T", "Robot targets.", GH_ParamAccess.list);
        _ = pManager.AddCurveParameter("Cut Paths", "C", "Cutting paths.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var target = DA.Get<Target>(1) as CartesianTarget
            ?? throw new ArgumentException("Reference target must be a cartesian target.");

        CSVConverterCore converter = new(
            DA.Get<string>(0),
            target,
            DA.Get<string>(2),
            DA.Get<bool>(3),
            DA.Get(4, 2750.0),
            DA.MaybeValue<Point3d>(5));

        DA.SetDataList(0, converter.Targets);
        DA.SetDataList(1, converter.ToolPath);
    }
}
