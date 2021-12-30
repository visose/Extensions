using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots.Grasshopper;

namespace Extensions.Grasshopper;

public class OrientToolpath : GH_Component
{
    public OrientToolpath() : base("Orient Toolpath", "GPath", "Orients a toolpath using a guide mesh and optional surface mesh.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Wave;
    public override Guid ComponentGuid => new Guid("{71B46D1D-C358-40F9-9726-FAB3F6E2AF0B}");

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
        pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Toolpath to orient.", GH_ParamAccess.item);
        pManager.AddMeshParameter("Surface", "S", "Surface mesh - mesh touching the toolpath.", GH_ParamAccess.item);
        pManager.AddMeshParameter("Guide", "G", "Guide mesh - mesh that will be used to align the normal of the targets.", GH_ParamAccess.item);
        pManager.AddPointParameter("Point alignment", "P", "Aligns the X axis of the tagets to look towards this point (in world coordinate system). If not supplied it will use the X axis of the reference target.", GH_ParamAccess.item);

        pManager[1].Optional = true;
        pManager[3].Optional = true;
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Oriented toolpath.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Toolpath toolpath = null;
        Mesh surface = null, guide = null;
        Point3d? point = null;

        if (!DA.GetData(0, ref toolpath)) return;
        DA.GetData(1, ref surface);
        if (!DA.GetData(2, ref guide)) return;
        DA.GetData(3, ref point);

        var alignment = Vector3d.XAxis;
        if (point.HasValue)
            alignment = (Vector3d)point.Value;

        var orientedToolpath = new Toolpaths.OrientToolpath(toolpath.Value, guide, alignment, surface).Toolpath;

        DA.SetData(0, orientedToolpath);
    }
}