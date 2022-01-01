using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Extensions.Document;
using Rhino.Display;

namespace Extensions.Grasshopper;

public class CreateDisplayGeometry : GH_Component
{
    public CreateDisplayGeometry() : base("Display Geometry", "DisGeo", "Attaches display attributes to geometry.", "Extensions", "Document") { }
    protected override Bitmap Icon => Util.GetIcon("Eye");
    public override Guid ComponentGuid => new Guid("{07955694-55A9-4AC6-88B8-A0F37632634B}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGeometryParameter("Geometry", "G", "Geometry.", GH_ParamAccess.item);
        pManager.AddParameter(new Param_OGLShader(), "Shader", "S", "Shader to attach to geometry.", GH_ParamAccess.item);
        pManager.AddTextParameter("Layer", "L", "Layer name.", GH_ParamAccess.item);
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new DisplayGeometryParameter(), "Display style", "D", "Display style.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        IGH_GeometricGoo geometry = null;
        DisplayMaterial material = null;
        string layer = "";

        if (!DA.GetData(0, ref geometry)) return;
        DA.GetData(1, ref material);
        DA.GetData(2, ref layer);

        var target = GH_Convert.ToGeometryBase(geometry);

        var displayStyle = new DisplayGeometry(target, material, layer);
        DA.SetData(0, new GH_DisplayGeometry(displayStyle));
    }
}
