using Extensions.Discrete;
using Rhino;

namespace Extensions.Grasshopper;

public class UnityExport() : Component(
    "Unity Export",
    "UnityExport",
    "Exports block instances for Unity.",
    "Discrete",
    "{09694580-A4BB-4CD8-B061-E158BC83478F}",
    "Puzzle")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddTextParameter("Block Names", "B", "Block names to export.", GH_ParamAccess.list);
        _ = pManager.AddTextParameter("Instance Layer", "I", "Layer containing block instances.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Density", "D", "Tile density.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Angle Limit", "Ja", "Maximum joint rotation angle. Use 0 for rigid joints.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Break Force", "Jf", "Joint break force in newtons.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("File Name", "F", "XML file path.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        Assembly.Export(
            [.. DA.List<string>(0)],
            DA.Get<string>(1),
            DA.Get<double>(2),
            DA.Get<double>(3),
            DA.Get<double>(4),
            DA.Get<string>(5),
            RhinoDoc.ActiveDoc);
    }
}
