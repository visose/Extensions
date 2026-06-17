using Grasshopper.Kernel.Parameters;
using Extensions.Document;

namespace Extensions.Grasshopper;

public class Export() : Component(
    "Export Geometry",
    "Export",
    "Exports display geometry from Grasshopper.",
    "Document",
    "{B1DF48A5-8BC1-4FB1-B284-7EA22725CABA}",
    "Save")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddParameter(new DisplayGeometryParameter(), "Display Geometry", "D", "Display geometry to export.", GH_ParamAccess.list);
        _ = pManager.AddIntegerParameter("Export Type", "E", "Export format.", GH_ParamAccess.item, 0);
        _ = pManager.AddTextParameter("Folder", "F", "Folder to export to.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("File Name", "N", "File name without extension.", GH_ParamAccess.item);

        var param = (Param_Integer)pManager[1];
        param.AddNamedValue("HTML (WebGL model, requires the Iris plugin)", 0);
        param.AddNamedValue("FBX (flips YZ components for Unity interop)", 1);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddTextParameter("File Path", "F", "Exported file path.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var filePath = IO.Export(
            [.. DA.List<DisplayGeometry>(0)],
            (IO.ExportType)DA.Get<int>(1),
            DA.Get<string>(2),
            DA.Get<string>(3));

        DA.SetData(0, filePath);
    }
}
