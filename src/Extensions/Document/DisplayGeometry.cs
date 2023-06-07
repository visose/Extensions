﻿using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino;

namespace Extensions.Document;

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
        att ??= doc.CreateDefaultAttributes();

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

            if (Material is not null)
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

                if (texture is not null)
                    material.SetTexture(texture, TextureType.Diffuse);

                var matIndex = doc.Materials.Add(material);
                att.MaterialIndex = matIndex;
            }
        }
        else if (Material is not null)
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
