using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;

namespace Extensions.Grasshopper;

public class CreateExtrusionAttributes() : RobotComponent(
    "Extrusion Attributes",
    "ExtAtt",
    "Creates extrusion toolpath settings.",
    "Toolpaths",
    "{0D176FFA-75B1-484A-A6C7-273492F8F53E}",
    "LayersConfig")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddParameter(new TargetParameter(), "Reference Target", "T", "Reference joint target.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Nozzle Diameter", "D", "Nozzle diameter.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Layer Height", "H", "Layer height.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Safe Z Offset", "Z", "Vertical safety offset.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new SpeedParameter(), "Approach Speed", "As", "Approach speed.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new SpeedParameter(), "Extrusion Speed", "Es", "Extrusion speed.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ZoneParameter(), "Approach Zone", "Az", "Approach zone.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ZoneParameter(), "Extrusion Zone", "Ez", "Extrusion zone.", GH_ParamAccess.item);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion Attributes", "A", "Extrusion toolpath settings.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var target = DA.Get<Target>(0) as JointTarget
            ?? throw new ArgumentException("Reference target must be a joint target.");

        var attributes = new ExtrusionAttributes
        {
            NozzleDiameter = DA.Get<double>(1),
            LayerHeight = DA.Get<double>(2),
            SafeZOffset = DA.Get<double>(3),
            SafeSpeed = target.Speed,
            ApproachSpeed = DA.Get<Speed>(4),
            ExtrusionSpeed = DA.Get<Speed>(5),
            SafeZone = target.Zone,
            ApproachZone = DA.Get<Zone>(6),
            ExtrusionZone = DA.Get<Zone>(7),
            Tool = target.Tool,
            Frame = target.Frame,
            Home = target.Joints
        }.Initialize();

        DA.SetData(0, attributes);
    }
}
