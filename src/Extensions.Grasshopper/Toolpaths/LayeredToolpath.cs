using Rhino.Geometry;
using ColumnCore = Extensions.Toolpaths.Column;

namespace Extensions.Grasshopper;

public class LayeredToolpath() : Component(
    "Layered Toolpath",
    "LayeredToolpath",
    "Creates layered extrusion preview geometry.",
    "Toolpaths",
    "{82C1EFE1-97C3-438C-84F4-C23F92574373}",
    "Layers")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Mesh", "M", "Mesh to slice.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Nozzle Diameter", "D", "Nozzle diameter.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Layer Height", "H", "Layer height.", GH_ParamAccess.item);
        _ = pManager.AddIntervalParameter("Region", "R", "Normalized slicing interval.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddCurveParameter("Contours", "O", "Original contours.", GH_ParamAccess.list);
        _ = pManager.AddCurveParameter("Paths", "C", "Cleaned deposition paths.", GH_ParamAccess.list);
        _ = pManager.AddMeshParameter("Beads", "P", "Bead preview meshes.", GH_ParamAccess.list);
        _ = pManager.AddMeshParameter("Skin", "S", "Skin preview mesh.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        ColumnCore column = new(
            DA.Get<Mesh>(0),
            DA.Get<double>(1),
            DA.Get<double>(2),
            DA.Get<Interval>(3));

        DA.SetDataList(0, column.Contours.Select(static p => new PolylineCurve(p)));
        DA.SetDataList(1, column.Layers.SelectMany(static p => p.Select(static c => new PolylineCurve(c))));
        DA.SetDataList(2, column.Pipes.SelectMany(static p => p));
        DA.SetData(3, column.Skin);
    }
}
