using System.Net;
using Rhino.UI;
using Rhino;

namespace Extensions.Document;

public static class IO
{
    public enum ExportType { HTML, FBX };

    public static string Export(IReadOnlyList<DisplayGeometry> geometries, ExportType exportType, string folder, string fileName)
    {
        var doc = RhinoDoc.ActiveDoc;
        List<Guid> guids = new(geometries.Count);

        bool flipYZ = exportType == ExportType.FBX;

        foreach (var geometry in geometries)
        {
            guids.Add(geometry.Bake(doc, doc.CreateDefaultAttributes(), flipYZ));
        }

        doc.Objects.UnselectAll(false);
        doc.Objects.Select(guids, true);

        string filePath = Path.Combine(folder, fileName);

        switch (exportType)
        {
            case ExportType.HTML:
                {
                    RhinoApp.RunScript($"-_Export \"{filePath}.html\" ui=yes launch=yes _Enter", false);
                    break;
                }
            case ExportType.FBX:
                {
                    RhinoApp.RunScript($"-_Export \"{filePath}.fbx\" _Enter _Enter", false);
                    break;
                }
            default:
                break;
        }

        doc.Objects.Delete(guids, true);

        return filePath;
    }

    static Task? _uploadTask;

    public static void FtpUpload(string localFilePath, string url, string user, string password)
    {
        if (_uploadTask != null && !_uploadTask.IsCompleted)
        {
            RhinoApp.WriteLine("Please wait until the last upload has completed.");
            return;
        }

        StatusBar.ShowProgressMeter(0, 100, "Uploading to FTP server...", true, true);

        _uploadTask = Task.Run(() =>
        {
            var fileName = Path.GetFileName(localFilePath);
            string webFilePath = $"{url}/{fileName}";

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(webFilePath);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.UploadFile;
            NetworkCredential credentials = new(user, password);
            request.Credentials = credentials;

            using (var inputStream = File.OpenRead(localFilePath))
            using (var outputStream = request.GetRequestStream())
            {
                var buffer = new byte[1024 * 1024];
                int totalReadBytesCount = 0;
                int readBytesCount;
                while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outputStream.Write(buffer, 0, readBytesCount);
                    totalReadBytesCount += readBytesCount;
                    var progress = totalReadBytesCount * 100.0 / inputStream.Length;

                    Action upload = () => StatusBar.UpdateProgressMeter((int)progress, true);
                    RhinoApp.InvokeOnUiThread(upload);
                }
            }

            RhinoApp.InvokeOnUiThread(new Action(() => StatusBar.HideProgressMeter()));

            using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Action text = () => RhinoApp.WriteLine($"Web upload of file '{fileName}.html' complete, status: {response.StatusDescription}");
            RhinoApp.InvokeOnUiThread(text);
        });
    }
}
