using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;

namespace Extensions.Grasshopper;

public class CreateExtrusionVisualizer() : RobotComponent(
    "Extrusion Visualizer",
    "ExtView",
    "Visualizes deposited extrusion material.",
    "Toolpaths",
    "{B6FCD119-0805-4FFF-A8AA-F18A37C4FC2F}",
    "LayersDraw")
{
    ExtrusionVisualizer? _visualizer;

    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddParameter(new ProgramParameter(), "Program", "P", "Robot program.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion Attributes", "A", "Extrusion toolpath settings.", GH_ParamAccess.item);
        _ = pManager.AddBooleanParameter("World Coordinates", "W", "Display meshes in world coordinates.", GH_ParamAccess.item, false);
        _ = pManager.AddIntegerParameter("Segments", "S", "Cross-section segment count.", GH_ParamAccess.item, 24);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Meshes", "M", "Extrusion preview meshes.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var program = DA.Get<IProgram>(0) as Program
            ?? throw new ArgumentException("Input program cannot have custom code.");

        var attributes = DA.Get<ExtrusionAttributes>(1);
        var segments = DA.Get<int>(3);

        if (_visualizer is null || _visualizer.Program != program)
        {
            _visualizer = new(
                program,
                attributes.BeadWidth,
                attributes.LayerHeight,
                attributes.ExtrusionZone.Distance,
                segments);
        }

        _visualizer.Update();

        DA.SetDataList(0, _visualizer.ExtrudedContours);
    }
}
