using Grasshopper.Kernel;
using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;

namespace Extensions.Grasshopper;

public class CreateExtrusionVisualizer : GH_Component
{
    public CreateExtrusionVisualizer() : base("Extrusion Visualizer", "ExtView", "Display extruded toolpath.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("LayersDraw");
    public override Guid ComponentGuid => new("{B6FCD119-0805-4FFF-A8AA-F18A37C4FC2F}");

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
        pManager.AddParameter(new ProgramParameter(), "Program", "P", "Robot program.", GH_ParamAccess.item);
        pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion attributes", "A", "Extrusion attributes.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("World Coord", "W", "Display in local or world coordinates.", GH_ParamAccess.item, false);
        pManager.AddIntegerParameter("Segments", "S", "Number of segments per crossection.", GH_ParamAccess.item, 24);
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("Meshes", "M", "Extruded toolpath.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Program program = null;
        GH_ExtrusionAttributes attributes = null;
        bool isWorld = false;
        int segments = 0;

        if (!DA.GetData(0, ref program)) return;
        if (!DA.GetData(1, ref attributes)) return;
        if (!DA.GetData(2, ref isWorld)) return;
        if (!DA.GetData(3, ref segments)) return;

        if (program?.Value is not Program p)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input program can't have custom code.");
            return;
        }

        if (_visualizer == null || _visualizer.Program != p)
        {
            _visualizer = new ExtrusionVisualizer(p, attributes.Value.BeadWidth, attributes.Value.LayerHeight, attributes.Value.ExtrusionZone.Distance, segments);
        }

        _visualizer.Update();

        DA.SetDataList(0, _visualizer.ExtrudedContours);
    }

    ExtrusionVisualizer _visualizer;
}
