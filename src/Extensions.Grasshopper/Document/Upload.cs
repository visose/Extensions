using Extensions.Document;

namespace Extensions.Grasshopper;

public class Upload() : Component(
    "Upload File",
    "Upload",
    "Uploads a file to an FTP server.",
    "Document",
    "{DCA98559-B42A-4FEB-9226-E182977D8228}",
    "CloudUpload")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddTextParameter("File Path", "F", "File path to upload.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("Address", "A", "Server address.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("User", "U", "User name.", GH_ParamAccess.item);
        _ = pManager.AddTextParameter("Password", "P", "Password.", GH_ParamAccess.item);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        IO.FtpUpload(
            DA.Get<string>(0),
            DA.Get<string>(1),
            DA.Get<string>(2),
            DA.Get<string>(3));
    }
}
