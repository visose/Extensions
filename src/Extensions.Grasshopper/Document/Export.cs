using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Extensions.Document;
using System.Drawing;

namespace Extensions.Grasshopper;

public class Export : GH_Component
{
    public Export() : base("Export Geometry", "Export", "Export Grasshopper geometry.", "Extensions", "Document") { }
    protected override Bitmap Icon => Properties.Resources.Save;
    public override Guid ComponentGuid => new Guid("{B1DF48A5-8BC1-4FB1-B284-7EA22725CABA}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new DisplayGeometryParameter(), "Display style", "D", "Display style.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Export type", "E", "Export type.", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Folder", "F", "Folder to export to. End without backslash.", GH_ParamAccess.item);
        pManager.AddTextParameter("File name", "N", "File name. Omit the extension.", GH_ParamAccess.item);
        var param = pManager[1] as Param_Integer;
        param.AddNamedValue("HTML (WebGL model, requires the Iris plugin)", 0);
        param.AddNamedValue("FBX (flips YZ components for Unity interop)", 1);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("File name", "F", "Exported file name.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var geometries = new List<GH_DisplayGeometry>();
        int exportType = 0;
        string folder = "";
        string fileName = "";

        if (!DA.GetDataList(0, geometries)) return;
        if (!DA.GetData(1, ref exportType)) return;
        if (!DA.GetData(2, ref folder)) return;
        if (!DA.GetData(3, ref fileName)) return;

        string filePath = IO.Export(geometries.Select(g => g.Value).ToList(), (IO.ExportType)exportType, folder, fileName);

        DA.SetData(0, filePath);
    }
}
