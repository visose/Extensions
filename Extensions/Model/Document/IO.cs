using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Net;
using System.Threading.Tasks;
using Rhino.UI;
using Rhino.Display;
using Rhino;

namespace Extensions.Model.Document
{
    public static class IO
    {
        public enum ExportType { HTML, FBX };

        public static string Export(List<DisplayGeometry> geometries, ExportType exportType, string folder, string fileName)
        {
            var doc = RhinoDoc.ActiveDoc;
            var guids = new List<Guid>(geometries.Count);

            bool flipYZ = exportType == ExportType.FBX;

            foreach (var geometry in geometries)
            {
                guids.Add(geometry.Bake(doc, null, flipYZ));
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

        static Task _uploadTask;

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

                var request = (FtpWebRequest)WebRequest.Create(webFilePath);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(user, password);

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
                        Rhino.RhinoApp.InvokeOnUiThread(upload);
                    }
                }

                RhinoApp.InvokeOnUiThread(new Action(() => StatusBar.HideProgressMeter()));

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Action text = () => Rhino.RhinoApp.WriteLine($"Web upload of file '{fileName}.html' complete, status: {response.StatusDescription}");
                    RhinoApp.InvokeOnUiThread(text);
                }
            });
        }
    }
}