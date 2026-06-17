using Grasshopper.Kernel.Parameters;
using Rhino.Display;
using Rhino.Geometry;
using Extensions.Document;

namespace Extensions.Grasshopper;

public class CreateDisplayGeometry() : Component(
    "Display Geometry",
    "DisGeo",
    "Attaches display settings to geometry.",
    "Document",
    "{07955694-55A9-4AC6-88B8-A0F37632634B}",
    "Eye")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddGeometryParameter("Geometry", "G", "Geometry to display.", GH_ParamAccess.item);
        _ = pManager.AddParameter(new Param_OGLShader(), "Shader", "S", "Optional display shader.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("Layer", "L", "Layer name.", GH_ParamAccess.item);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddParameter(new DisplayGeometryParameter(), "Display Geometry", "D", "Display geometry.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        DisplayGeometry displayStyle = new(
            DA.Get<GeometryBase>(0),
            DA.Maybe<DisplayMaterial>(1),
            DA.Get(2, ""));

        DA.SetData(0, displayStyle);
    }
}
