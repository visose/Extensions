using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Net;
using System.Threading.Tasks;
using Rhino.UI;

namespace Extensions.Model.Document
{
    public static class IO
    {
        public static string Export(List<DisplayStyle> geometries, int exportType, string folder, string fileName)
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var guids = new List<Guid>(geometries.Count);

            foreach (var geometry in geometries)
            {
                int layerIndex;

                var layer = doc.Layers.FindName(geometry.Layer, Rhino.RhinoMath.UnsetIntIndex);

                if (layer == null)
                {
                    layerIndex = doc.Layers.Add(new Layer() { Name = geometry.Layer });
                }
                else
                {
                    layerIndex = layer.Index;
                }

                var att = new ObjectAttributes
                {
                    ColorSource = ObjectColorSource.ColorFromObject,
                    ObjectColor = geometry.Color,
                    LayerIndex = layerIndex
                };

                guids.Add(doc.Objects.Add(geometry.Geometry, att));
            }

            string filePath = Path.Combine(folder, $"{fileName}.html");
            Rhino.RhinoApp.RunScript($"-_SaveAs \"{filePath}\" ui=yes launch=yes _Enter", false);
            doc.Objects.Delete(guids, true);

            return filePath;
        }

        static Task _uploadTask;

        public static void FtpUpload(string localFilePath, string url, string user, string password)
        {
            if (_uploadTask != null && !_uploadTask.IsCompleted)
            {
                Rhino.RhinoApp.WriteLine("Please wait until the last upload has completed.");
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

                Rhino.RhinoApp.InvokeOnUiThread(new Action(() => StatusBar.HideProgressMeter()));

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Action text = () => Rhino.RhinoApp.WriteLine($"Web upload of file '{fileName}.html' complete, status: {response.StatusDescription}");
                    Rhino.RhinoApp.InvokeOnUiThread(text);
                }
            });
        }
    }

    public class DisplayStyle
    {
        public GeometryBase Geometry { get; set; }
        public Color Color { get; set; }
        public string Layer { get; set; }

        public DisplayStyle(GeometryBase geometry, Color color, string layer = "")
        {
            Geometry = geometry;
            Color = color;
            Layer = layer;
        }

        public override string ToString() => $"Display Style ({Geometry.ObjectType}, {Color.Name}, {Layer})";
    }
}