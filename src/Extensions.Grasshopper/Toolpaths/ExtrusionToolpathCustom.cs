using Grasshopper.Kernel;
using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Extensions.Grasshopper;

public class CreateExternalExtrusionToolpathCustom : GH_Component
{
    public CreateExternalExtrusionToolpathCustom() : base("Extrusion Toolpath Ex Custom", "ExtPathCustom", "Extrusion toolpath with external axis with custom plane and external value.", "Extensions", "Toolpaths") { }
    public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.LayersAdd;
    public override Guid ComponentGuid => new Guid("{5219D611-92BF-42AA-95E5-AEA4115D360A}");

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
        pManager.AddPlaneParameter("Planes", "P", "Lists of target locations as planes.", GH_ParamAccess.tree);
        pManager.AddNumberParameter("Lengths", "L", "Raw values for external axis (extrusion amount per target)", GH_ParamAccess.tree);
        pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion attributes", "A", "Extrusion attributes.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Extrusion Factor", "F", "Extrusion factor.", GH_ParamAccess.item, 0.21);
        pManager.AddNumberParameter("Suck Back", "Sb", "Distance to extrude in reverse immediately after stopping the extrusion. To avoid dripping material.", GH_ParamAccess.item, 0);
        pManager.AddNumberParameter("Start Distance", "Sd", "Distance to extrude before the robot starts moving. To compensate for material not flowing immediately after starting the extrusion.", GH_ParamAccess.item, 0);
        pManager.AddNumberParameter("Test Loop", "L", "Distance for test loop.", GH_ParamAccess.item, 200);
    }

    void Outputs(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Extrusion toolpath.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Layer indices", "L", "Layer indices.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var locationsGH = new GH_Structure<GH_Plane>();
        var lengthsGH = new GH_Structure<GH_Number>();

        GH_ExtrusionAttributes attributes = null;
        double factor = 0, suckBack = 0, loop = 0, startDistance = 0;

        if (!DA.GetDataTree(0, out locationsGH)) return;
        if (!DA.GetDataTree(1, out lengthsGH)) return;
        if (!DA.GetData(2, ref attributes)) return;
        if (!DA.GetData(3, ref factor)) return;
        if (!DA.GetData(4, ref suckBack)) return;
        if (!DA.GetData(5, ref startDistance)) return;
        if (!DA.GetData(6, ref loop)) return;

        var locations = locationsGH.Branches.Select(b => b.Select(p => p.Value).ToList()).ToList();
        var lengths = lengthsGH.Branches.Select(b => b.Select(p => p.Value).ToList()).ToList();

        var toolpath = new ExternalExtrusionToolpath(locations, lengths, attributes.Value, factor, suckBack, startDistance, loop);

        DA.SetData(0, toolpath);
        DA.SetDataList(1, toolpath.SubPrograms);
    }
}
