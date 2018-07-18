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

    public class DisplayGeometry
    {
        public GeometryBase Geometry { get; set; }
        public DisplayMaterial Material { get; set; }
        public string Layer { get; set; }

        public DisplayGeometry(GeometryBase geometry, DisplayMaterial material, string layer = "")
        {
            Geometry = geometry;
            Material = material;
            Layer = layer;
        }

        public Guid Bake(RhinoDoc doc, ObjectAttributes att = null, bool flipYZ = false)
        {
            if (att == null) att = doc.CreateDefaultAttributes();

            if (!string.IsNullOrEmpty(Layer))
            {
                var layer = doc.Layers.FindName(Layer, RhinoMath.UnsetIntIndex);

                if (layer == null)
                {
                    att.LayerIndex = doc.Layers.Add(new Layer() { Name = Layer });
                }
                else
                {
                    att.LayerIndex = layer.Index;
                }
            }

            var geometry = Geometry;

            if (Geometry is Mesh)
            {
                if (flipYZ)
                    geometry = (Geometry as Mesh).FlipYZ();

                if (Material != null)
                {
                    att.ColorSource = ObjectColorSource.ColorFromMaterial;
                    att.MaterialSource = ObjectMaterialSource.MaterialFromObject;

                    double transparency = Material.Transparency;
                    if (flipYZ) transparency = 1 - transparency;

                    var material = new Material
                    {
                        DiffuseColor = Material.Diffuse,
                        EmissionColor = Material.Emission,
                        Transparency = transparency
                    };

                    var texture = Material.GetBitmapTexture(true);

                    if (texture != null)
                        material.SetBitmapTexture(texture);

                    var matIndex = doc.Materials.Add(material);
                    att.MaterialIndex = matIndex;
                }
            }
            else if (Material != null)
            {
                att.ColorSource = ObjectColorSource.ColorFromObject;
                att.ObjectColor = Material.Diffuse;
            }

            return doc.Objects.Add(geometry, att);
        }

        public DisplayGeometry Duplicate()
        {
            return new DisplayGeometry(Geometry.Duplicate(), new DisplayMaterial(Material), Layer);
        }

        public DisplayGeometry Transform(Transform xform)
        {
            var copy = Duplicate();
            copy.Geometry.Transform(xform);
            return copy;
        }

        public override string ToString() => $"Display Geometry ({Geometry.ObjectType}, {Material?.Diffuse.Name ?? "Default"}, {Layer ?? "Active"})";
    }
}