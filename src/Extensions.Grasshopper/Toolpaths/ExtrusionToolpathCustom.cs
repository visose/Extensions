using Robots.Grasshopper;
using Extensions.Toolpaths.Extrusion;

namespace Extensions.Grasshopper;

public class CreateExternalExtrusionToolpathCustom() : RobotComponent(
    "Custom External Extrusion Toolpath",
    "ExtPathCustom",
    "Creates an extrusion toolpath from target planes and external-axis values.",
    "Toolpaths",
    "{5219D611-92BF-42AA-95E5-AEA4115D360A}",
    "LayersAdd")
{
    protected override void RegisterRobotInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddPlaneParameter("Planes", "P", "Target planes by path.", GH_ParamAccess.tree);
        _ = pManager.AddNumberParameter("Lengths", "L", "External-axis values by target.", GH_ParamAccess.tree);
        _ = pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion Attributes", "A", "Extrusion toolpath settings.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Extrusion Factor", "F", "External-axis extrusion scale.", GH_ParamAccess.item, 0.21);
        _ = pManager.AddNumberParameter("Suck Back", "Sb", "Reverse extrusion distance after stopping.", GH_ParamAccess.item, 0);
        _ = pManager.AddNumberParameter("Start Distance", "Sd", "Extrusion distance before robot motion starts.", GH_ParamAccess.item, 0);
        _ = pManager.AddNumberParameter("Test Loop", "L", "Test loop distance.", GH_ParamAccess.item, 200);
    }

    protected override void RegisterRobotOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Robot toolpath.", GH_ParamAccess.item);
        _ = pManager.AddIntegerParameter("Layer Indices", "L", "First target index of each layer.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        ExternalExtrusionToolpath toolpath = new(
            DA.PlaneTree(0),
            DA.NumberTree(1),
            DA.Get<ExtrusionAttributes>(2),
            DA.Get<double>(3),
            DA.Get<double>(4),
            DA.Get<double>(5),
            DA.Get<double>(6));

        DA.SetData(0, toolpath);
        DA.SetDataList(1, toolpath.SubPrograms);
    }
}
