using Grasshopper.Kernel;
using Robots;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;
using Grasshopper.Kernel.Types;

namespace Extensions.Grasshopper;

public class CreateExtrusionAttributes : GH_Component
{
    public CreateExtrusionAttributes() : base("Extrusion Attributes", "ExtAtt", "Extrusion attributes.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("LayersConfig");
    public override Guid ComponentGuid => new Guid("{0D176FFA-75B1-484A-A6C7-273492F8F53E}");

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
        pManager.AddParameter(new TargetParameter(), "Reference target", "T", "Create targets will inherit the tool and frame from this target.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Nozzle diameter", "D", "Nozzle diameter.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Layer height", "H", "Layer height.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Safe Z offset", "Z", "Vertical margin to add to approaches.", GH_ParamAccess.item);
        pManager.AddParameter(new SpeedParameter(), "Approach speed", "As", "Approach speed.", GH_ParamAccess.item);
        pManager.AddParameter(new SpeedParameter(), "Extrusion speed", "Es", "Extrusion speed.", GH_ParamAccess.item);
        pManager.AddParameter(new ZoneParameter(), "Approach zone", "Az", "Approach zone.", GH_ParamAccess.item);
        pManager.AddParameter(new ZoneParameter(), "Extrusion zone", "Ez", "Extrusion zone.", GH_ParamAccess.item);
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion Attributes", "A", "Extrusion attributes.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int inputCount = 8;
        var inputs = new IGH_Goo[inputCount];

        for (int i = 0; i < inputCount; i++)
        {
            if (!DA.GetData(i, ref inputs[i])) return;
        }

        var target = (inputs[0] as GH_Target).Value as JointTarget;

        var attributes = new ExtrusionAttributes()
        {
            NozzleDiameter = (inputs[1] as GH_Number).Value,
            LayerHeight = (inputs[2] as GH_Number).Value,
            SafeZOffset = (inputs[3] as GH_Number).Value,
            SafeSpeed = target.Speed,
            ApproachSpeed = (inputs[4] as GH_Speed).Value,
            ExtrusionSpeed = (inputs[5] as GH_Speed).Value,
            SafeZone = target.Zone,
            ApproachZone = (inputs[6] as GH_Zone).Value,
            ExtrusionZone = (inputs[7] as GH_Zone).Value,
            Tool = target.Tool,
            Frame = target.Frame,
            Home = target.Joints
        }.Initialize();

        DA.SetData(0, new GH_ExtrusionAttributes(attributes));
    }
}
