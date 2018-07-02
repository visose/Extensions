using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Extensions.Model.Document
{
    static class Debug
    {
        public static void Bake<T>(T geometry, Color? color = null)
        {
            const string layerName = "Debug";
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var layer = doc.Layers.FindName(layerName);

            if (layer == null)
            {
                var layerIndex = doc.Layers.Add(layerName, Color.Red);
                layer = doc.Layers.FindIndex(layerIndex);
            }

            var att = new ObjectAttributes
            {
                LayerIndex = layer.Index
            };

            if (color != null)
            {
                att.ColorSource = ObjectColorSource.ColorFromObject;
                att.ObjectColor = (Color)color;
            }

            switch (geometry)
            {
                case GeometryBase geo:
                    doc.Objects.Add(geo, att);
                    break;
                case Polyline pl:
                    doc.Objects.AddPolyline(pl, att);
                    break;
                case IEnumerable<Point3d> pc:
                    doc.Objects.AddPointCloud(new PointCloud(pc), att);
                    break;
                case Line l:
                    doc.Objects.AddLine(l, att);
                    break;
                default:
                    throw new ArgumentException(" Geometry type not recognized.");
            }
        }
    }
}
