using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.Milling;

namespace Extensions.Grasshopper;

public class CreateMillingAttributes() : RobotComponent(
    "Milling Attributes",
    "MillAtt",
    "Creates milling toolpath settings.",
    "Toolpaths",
    "{F6A3EFA3-2CC5-4E17-BEE8-E8B9AA6648B5}",
    "LayersConfig")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddParameter(new TargetParameter(), "Reference Target", "T", "Reference joint target.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("End Mill Diameter", "D", "End mill diameter.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("End Mill Length", "L", "End mill length.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Step Over", "So", "Step over in mm.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Step Down", "Sd", "Step down in mm.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Safe Z Offset", "Z", "Vertical safety offset.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new SpeedParameter(), "Plunge Speed", "Ps", "Plunge speed.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new SpeedParameter(), "Cut Speed", "Cs", "Cut speed.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ZoneParameter(), "Plunge Zone", "Pz", "Plunge zone.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new ZoneParameter(), "Cut Zone", "Cz", "Cut zone.", GH_ParamAccess.item);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new MillingAttributesParameter(), "Milling Attributes", "A", "Milling toolpath settings.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var target = DA.Get<Target>(0) as JointTarget
            ?? throw new ArgumentException("Reference target must be a joint target.");

        var attributes = new MillingAttributes
        {
            EndMill = new()
            {
                Diameter = DA.Get<double>(1),
                Length = DA.Get<double>(2)
            },
            StepOver = DA.Get<double>(3),
            StepDown = DA.Get<double>(4),
            SafeZOffset = DA.Get<double>(5),
            SafeSpeed = target.Speed,
            PlungeSpeed = DA.Get<Speed>(6),
            CutSpeed = DA.Get<Speed>(7),
            SafeZone = target.Zone,
            PlungeZone = DA.Get<Zone>(8),
            CutZone = DA.Get<Zone>(9),
            Tool = target.Tool,
            Frame = target.Frame,
            Home = target.Joints
        }.Initialize();

        DA.SetData(0, attributes);
    }
}
