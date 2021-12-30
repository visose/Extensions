using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;

namespace Extensions.Grasshopper;

public class GCodeToolpath : GH_Component
{
    public GCodeToolpath() : base("G-code Toolpath", "GPath", "Converts a G-code file to robot targets.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Fingerprint;
    public override Guid ComponentGuid => new Guid("{862CF2F9-EF08-444C-88B5-459D365EB60A}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        if (ExtensionsInfo.IsRobotsInstalled)
            Inputs(pManager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        if (ExtensionsInfo.IsRobotsInstalled)
            Outputs(pManager);
    }

    void Inputs(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("File", "F", "GCode file.", GH_ParamAccess.item);
        pManager.AddParameter(new TargetParameter(), "Target", "T", "Created targets will use the parameters of this reference target if they're not supplied in the G-code file.", GH_ParamAccess.item);
        pManager.AddPointParameter("Point alignment", "P", "Aligns the X axis of the tagets to look towards this point (in world coordinate system). If not supplied it will use the X axis of the reference target.", GH_ParamAccess.item);
        pManager[2].Optional = true;
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Milling toolpath.", GH_ParamAccess.item);
        pManager.AddParameter(new ToolParameter(), "Spindle", "S", "Spindle with end mill.", GH_ParamAccess.item);
        pManager.AddPlaneParameter("MCS", "P", "Plane of machine coordinate system.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Rapid", "R", "Starting index of rapid moves.", GH_ParamAccess.list);
        pManager.AddTextParameter("Ignored", "I", "Ignored instructions.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string file = null;
        GH_Target target = null;
        Point3d? point = null;

        if (!DA.GetData(0, ref file)) return;
        if (!DA.GetData(1, ref target)) return;
        DA.GetData(2, ref point);

        var alignment = Vector3d.XAxis;
        if (point.HasValue)
            alignment = (Vector3d)point.Value;

        var toolpath = new Toolpaths.Milling.GCodeToolpath(file, target.Value as CartesianTarget, alignment);
        var (tool, mcs, rapidStarts, ignored) = toolpath.Toolpath;

        DA.SetData(0, toolpath);
        DA.SetData(1, tool);
        DA.SetData(2, mcs.Plane);
        DA.SetDataList(3, rapidStarts);
        DA.SetDataList(4, ignored);
    }
}
