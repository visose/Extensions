using Grasshopper.Kernel;
using Extensions.Document;
using System.Drawing;

namespace Extensions.Grasshopper;

public class Upload : GH_Component
{
    public Upload() : base("Upload File", "Upload", "Upload a file to the web.", "Extensions", "Document") { }
    protected override Bitmap Icon => Properties.Resources.CloudUpload;
    public override Guid ComponentGuid => new Guid("{DCA98559-B42A-4FEB-9226-E182977D8228}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("File path", "F", "Path of the file to upload.", GH_ParamAccess.item);
        pManager.AddTextParameter("Address", "A", "Server address.", GH_ParamAccess.item);
        pManager.AddTextParameter("User", "U", "User name.", GH_ParamAccess.item);
        pManager.AddTextParameter("Password", "P", "Password.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string filePath = "";
        string url = "";
        string user = "";
        string password = "";

        if (!DA.GetData(0, ref filePath)) return;
        if (!DA.GetData(1, ref url)) return;
        if (!DA.GetData(2, ref user)) return;
        if (!DA.GetData(3, ref password)) return;

        IO.FtpUpload(filePath, url, user, password);
    }
}
