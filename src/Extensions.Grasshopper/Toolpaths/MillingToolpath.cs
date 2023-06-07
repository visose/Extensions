using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots.Grasshopper;
using Extensions.Toolpaths.Milling;

namespace Extensions.Grasshopper;

public class CreateMillingToolpath : GH_Component
{
    public CreateMillingToolpath() : base("Milling Toolpath", "MillPath", "Milling toolpath.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("LayersSubtract");
    public override Guid ComponentGuid => new("{49D4E0A2-DD27-4EA4-A887-5E9AF27ECF45}");

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
        pManager.AddCurveParameter("Paths", "P", "Paths as as list of polylines.", GH_ParamAccess.list);
        pManager.AddBoxParameter("Bounding box.", "B", "Bounding box of the block.", GH_ParamAccess.item);
        pManager.AddParameter(new MillingAttributesParameter(), "Milling Attributes", "A", "Milling attributes.", GH_ParamAccess.item);
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ToolpathParameter(), "Targets", "T", "List of robot targets.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Layer indices", "L", "Layer indices.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_MillingAttributes attributes = null;
        var paths = new List<Curve>();
        var box = Box.Unset;

        if (!DA.GetDataList(0, paths)) return;
        if (!DA.GetData(1, ref box)) return;
        if (!DA.GetData(2, ref attributes)) return;

        var polylines = paths
            .Where(p => p.IsPolyline())
            .Select(p => { p.TryGetPolyline(out Polyline pl); return pl; })
            .ToList();

        var toolpath = new MillingToolpath(polylines, box.BoundingBox, attributes.Value);

        DA.SetData(0, toolpath);
        DA.SetDataList(1, toolpath.SubPrograms);
    }
}
