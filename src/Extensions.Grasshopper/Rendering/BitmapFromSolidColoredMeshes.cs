using Extensions.Document;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;


namespace Extensions.Grasshopper;

public class BitmapFromSolidColoredMeshes : GH_Component
{
    public BitmapFromSolidColoredMeshes() : base("Bitmap From Meshes", "BmpMeshes", "Creates a bitmap from solid colored meshes and assigns the corresponding uv coordinates to the meshes.", "Extensions", "Rendering") { }
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("PaintBrush02");
    public override Guid ComponentGuid => new Guid("{db4d6bc7-9e3c-459b-8494-6fd92dc526ca}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Meshes", "M", "One or more meshes, each with a unique vertex color.", GH_ParamAccess.list);
        pManager.AddTextParameter("File path", "F", "File path to a PNG image.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new DisplayGeometryParameter(), "Dispay mesh", "M", "Display geometry object with the uv coords of each mesh mapped to a pixel of the bitmap.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var meshes = new List<Mesh>();
        string file = string.Empty;
        DA.GetDataList(0, meshes);
        DA.GetData(1, ref file);

        var outMeshes = RenderExtensions.BitmapFromSolidColoredMeshes(meshes, file);

        var joinedMesh = new Mesh();
        foreach (var mesh in outMeshes)
            joinedMesh.Append(mesh);

        var material = new DisplayMaterial();
        material.SetBitmapTexture(file, true);

        var display = new DisplayGeometry(joinedMesh, material);
        DA.SetData(0, new GH_DisplayGeometry(display));
    }
}
