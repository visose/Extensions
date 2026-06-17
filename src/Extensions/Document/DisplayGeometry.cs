using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino;

namespace Extensions.Document;

public class DisplayGeometry(GeometryBase geometry, DisplayMaterial? material, string layer = "")
{
    public GeometryBase Geometry { get; } = geometry;
    public DisplayMaterial? Material { get; } = material;
    public string Layer { get; } = layer;

    public Guid Bake(RhinoDoc doc, ObjectAttributes? att = null, bool flipYZ = false)
    {
        att ??= doc.CreateDefaultAttributes();

        if (!string.IsNullOrEmpty(Layer))
        {
            var layer = doc.Layers.FindName(Layer, RhinoMath.UnsetIntIndex);

            att.LayerIndex = layer == null
                ? doc.Layers.Add(new Layer() { Name = Layer }) : layer.Index;
        }

        var geometry = Geometry;

        if (Geometry is Mesh mesh)
        {
            if (flipYZ)
                geometry = mesh.FlipYZ();

            if (Material is not null)
            {
                att.ColorSource = ObjectColorSource.ColorFromMaterial;
                att.MaterialSource = ObjectMaterialSource.MaterialFromObject;

                double transparency = Material.Transparency;
                if (flipYZ)
                    transparency = 1 - transparency;

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
        return new(Geometry.Duplicate(), Material is null ? null : new(Material), Layer);
    }

    public DisplayGeometry Transform(Transform xform)
    {
        var copy = Duplicate();
        copy.Geometry.Transform(xform);
        return copy;
    }

    public override string ToString() => $"Display Geometry ({Geometry.ObjectType}, {Material?.Diffuse.Name ?? "Default"}, {(string.IsNullOrEmpty(Layer) ? "Active" : Layer)})";
}
