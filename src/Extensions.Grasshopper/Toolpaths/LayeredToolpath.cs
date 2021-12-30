using Rhino.Geometry;
using Grasshopper.Kernel;

namespace Extensions.Grasshopper;

public class LayeredToolpath : GH_Component
{
    public LayeredToolpath() : base("Layered Toolpath", "LayeredToolpath", "Creates a layered extrusion toolpath.", "Extensions", "Toolpaths") { }
    protected override System.Drawing.Bitmap Icon => Properties.Resources.Layers;
    public override Guid ComponentGuid => new Guid("{82C1EFE1-97C3-438C-84F4-C23F92574373}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Mesh.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Nozzle diameter", "D", "Nozzle diameter", GH_ParamAccess.item);
        pManager.AddNumberParameter("Layer height", "H", "Layer height", GH_ParamAccess.item);
        pManager.AddIntervalParameter("Region", "R", "Region", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Original", "O", "Original contours.", GH_ParamAccess.list);
        pManager.AddCurveParameter("Clean", "C", "Cleaned contours.", GH_ParamAccess.list);
        pManager.AddMeshParameter("Pipe", "P", "3D beads.", GH_ParamAccess.list);
        pManager.AddMeshParameter("Skin", "S", "3D skin for visualization.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        Mesh mesh = new Mesh();
        double diameter = 0, height = 0;
        Interval region = Interval.Unset;

        DA.GetData(0, ref mesh);
        DA.GetData(1, ref diameter);
        DA.GetData(2, ref height);
        DA.GetData(3, ref region);

        var column = new Toolpaths.Column(mesh, diameter, height, region);

        var original = column.Contours.Select(p => new PolylineCurve(p));
        var contours = column.Layers.SelectMany(p => p.Select(c => new PolylineCurve(c)));
        var pipes = column.Pipes.SelectMany(p => p);
        var skin = column.Skin;

        DA.SetDataList(0, original);
        DA.SetDataList(1, contours);
        DA.SetDataList(2, pipes);
        DA.SetData(3, skin);
    }
}
