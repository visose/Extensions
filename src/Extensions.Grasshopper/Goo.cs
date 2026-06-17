using System.Drawing;
using Grasshopper.Kernel.Types;
using Extensions.Document;
using Extensions.Toolpaths.Milling;
using Extensions.Toolpaths.Extrusion;
using Rhino.Geometry;
using Rhino.Display;
using Rhino;
using Rhino.DocObjects;
using Rhino.Render;

namespace Extensions.Grasshopper;

public class GH_DisplayGeometry : GH_GeometricGoo<DisplayGeometry>, IGH_PreviewData, IGH_BakeAwareData, IGH_RenderAwareData
{
    public GH_DisplayGeometry() { }
    public GH_DisplayGeometry(GH_DisplayGeometry goo)
    {
        if (goo.Value is not null)
            Value = goo.Value.Duplicate();
    }
    public GH_DisplayGeometry(DisplayGeometry native) : base(native) { Value = native; }

    public override IGH_Goo Duplicate() => new GH_DisplayGeometry(this);
    public override bool IsValid => Value is not null;
    public override string TypeName => "Display Style";
    public override string TypeDescription => "Display geometry.";
    public override string ToString() => Value?.ToString() ?? "Null Display Geometry";
    public override object? ScriptVariable() => Value;

    public override bool CastFrom(object source)
    {
        var target = GH_Convert.ToGeometryBase(source);

        if (target == null)
            return false;

        Value = new(target, new(Color.Black, 0));
        return true;
    }

    public override bool CastTo<Q>(ref Q target)
    {
        if (Value is null)
            return false;

        if (typeof(Q).IsAssignableFrom(typeof(DisplayGeometry)))
        {
            object ptr = Value;
            target = (Q)ptr;
            return true;
        }

        if (typeof(Q).IsAssignableFrom(typeof(GH_Material)))
        {
            if (Value.Material is null)
                return false;

            GH_Material ptr = new(Value.Material);
            target = (Q)(object)ptr;
            return true;
        }

        return false;
    }

    public override BoundingBox Boundingbox => Value?.Geometry.GetBoundingBox(true) ?? BoundingBox.Empty;

    public override IGH_GeometricGoo DuplicateGeometry()
    {
        return Value is null ? new GH_DisplayGeometry() : new GH_DisplayGeometry(Value.Duplicate());
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
        return Value?.Geometry.GetBoundingBox(xform) ?? BoundingBox.Empty;
    }

    public override IGH_GeometricGoo Transform(Transform xform)
    {
        return Value is null ? new GH_DisplayGeometry() : new GH_DisplayGeometry(Value.Transform(xform));
    }

    public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
    {
        if (Value is null)
            return new GH_DisplayGeometry();

        var copy = Value.Duplicate();
        xmorph.Morph(copy.Geometry);
        return new GH_DisplayGeometry(copy);
    }

    public BoundingBox ClippingBox => Boundingbox;

    public void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
        if (Value?.Geometry is Mesh mesh)
        {
            var material = Value.Material ?? args.Material;
            args.Pipeline.DrawMeshShaded(mesh, material);
        }
    }

    public void DrawViewportWires(GH_PreviewWireArgs args)
    {
        if (Value?.Geometry is Curve curve)
            args.Pipeline.DrawCurve(curve, Value.Material?.Diffuse ?? args.Color);
    }

    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
    {
        obj_guid = Guid.Empty;

        if (Value is null)
            return false;

        obj_guid = Value.Bake(doc, att);
        return true;
    }

    void IGH_RenderAwareData.AppendRenderGeometry(GH_RenderArgs args, RenderMaterial material)
    {
        if (Value?.Geometry is Mesh mesh)
        {
            var renderMat = material;

            if (Value.Material != null)
            {
                var mat = new Material
                {
                    DiffuseColor = Value.Material.Diffuse,
                    EmissionColor = Value.Material.Emission,
                    Transparency = Value.Material.Transparency
                };

                var texture = Value.Material.GetBitmapTexture(true);

                if (texture is not null)
                    mat.SetTexture(texture, TextureType.Diffuse);

                renderMat = mat.RenderMaterial;
            }

#pragma warning disable CS0612
            args.Geomety.Add(mesh, renderMat);
#pragma warning restore CS0612
        }
    }
}

public class GH_MillingAttributes() : Goo<MillingAttributes, GH_MillingAttributes>("Milling Attributes")
{
    public GH_MillingAttributes(MillingAttributes native) : this() { SetValue(native); }
}

public class GH_ExtrusionAttributes() : Goo<ExtrusionAttributes, GH_ExtrusionAttributes>("Extrusion Attributes")
{
    public GH_ExtrusionAttributes(ExtrusionAttributes native) : this() { SetValue(native); }
}

public class DisplayGeometryParameter : GH_Param<GH_DisplayGeometry>, IGH_PreviewObject, IGH_BakeAwareObject
{
    public DisplayGeometryParameter() : base("Display Geometry", "DisGeo", "Display geometry.", "Extensions", "Parameters", GH_ParamAccess.item) { }
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Util.GetIcon("EyeParam");
    public override Guid ComponentGuid => new("{9F90313D-5776-471C-9922-29D4F59A70C4}");

    protected override GH_DisplayGeometry PreferredCast(object data) =>
        data is DisplayGeometry display
            ? new(display)
            : base.PreferredCast(data);

    public bool Hidden { get; set; }
    public bool IsPreviewCapable => true;
    public BoundingBox ClippingBox => Preview_ComputeClippingBox();
    public void DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    public void DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    public bool IsBakeCapable => true;

    public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
    {
        BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
    }

    public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
    {
        foreach (var data in VolatileData.AllData(true).Cast<IGH_BakeAwareData>())
        {
            data.BakeGeometry(doc, att, out Guid id);
            obj_ids.Add(id);
        }
    }
}

public class ExtrusionAttributesParameter() : Param<ExtrusionAttributes, GH_ExtrusionAttributes>(
    "Extrusion Attributes",
    "Extrusion toolpath settings.",
    "{D63464DC-BBAB-4A88-923A-8D9381FB1D0B}",
    "LayersConfigParam");

public class MillingAttributesParameter() : Param<MillingAttributes, GH_MillingAttributes>(
    "Milling Attributes",
    "Milling toolpath settings.",
    "{21255B77-9E3D-43BD-8FE5-9A77D4A4D575}",
    "LayersConfigParam");
