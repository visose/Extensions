using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using OrientToolpathCore = Extensions.Toolpaths.OrientToolpath;

namespace Extensions.Grasshopper;

public class OrientToolpath() : RobotComponent(
    "Orient Toolpath",
    "OrientPath",
    "Orients a toolpath using a guide mesh and optional surface mesh.",
    "Toolpaths",
    "{71B46D1D-C358-40F9-9726-FAB3F6E2AF0B}",
    "Wave")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Robot toolpath to orient.", GH_ParamAccess.item);
        _ = pManager.AddMeshParameter("Surface", "S", "Optional surface mesh for closest-point projection.", GH_ParamAccess.item);
        _ = pManager.AddMeshParameter("Guide", "G", "Guide mesh used to align target normals.", GH_ParamAccess.item);
        _ = pManager.AddPointParameter("Point Alignment", "P", "Optional point used to align target X axes.", GH_ParamAccess.item);

        pManager[1].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Oriented robot toolpath.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var alignment = DA.MaybeValue<Point3d>(3) is { } point
            ? (Vector3d)point
            : Vector3d.XAxis;

        OrientToolpathCore orientedToolpath = new(
            DA.Get<IToolpath>(0),
            DA.Get<Mesh>(2),
            alignment,
            DA.Maybe<Mesh>(1));

        DA.SetData(0, orientedToolpath.Toolpath);
    }
}
