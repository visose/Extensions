using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.Geometry;
using Extensions.Model.Spatial;
using MoreLinq;
using static System.Math;

namespace Extensions.Model.Document
{
    static class Cache
    {
        const string _layerName = "Cache";

        public static T Read<T>(string name) where T : GeometryBase
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var layer = doc.Layers.FindName(_layerName);

            if (layer is null)
                return null;

            return Array.Find(doc.Objects.FindByLayer(layer), o => o.Name == name && o.Geometry is T)
                    ?.Geometry as T;
        }

        public static void Write<T>(string name, T geometry) where T: GeometryBase
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var layer = doc.Layers.FindName(_layerName);

            if (layer is null)
            {
                int index = doc.Layers.Add(_layerName, Color.Black);
                layer = doc.Layers.FindIndex(index);
            }

            var attributes = new Rhino.DocObjects.ObjectAttributes
            {
                LayerIndex = layer.Index,
                Name = name
            };

            doc.Objects.Add(geometry, attributes);
       }
    }
}
