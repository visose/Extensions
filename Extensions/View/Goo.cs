using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Extensions.Model.Document;
using Extensions.Model.Toolpaths.Milling;
using Extensions.Model.Toolpaths.Extrusion;
using Rhino.Geometry;
using Rhino.Display;
using Rhino;
using Rhino.DocObjects;
using Rhino.Render;

namespace Extensions.View
{
    public class GH_DisplayGeometry : GH_GeometricGoo<DisplayGeometry>, IGH_PreviewData, IGH_BakeAwareData, IGH_RenderAwareData
    {
        public GH_DisplayGeometry() { Value = new DisplayGeometry(null, new DisplayMaterial(Color.Black, 0)); }
        public GH_DisplayGeometry(GH_DisplayGeometry goo) { Value = goo.Value; }
        public GH_DisplayGeometry(DisplayGeometry native) : base(native) { Value = native; }

        public override IGH_Goo Duplicate() => new GH_DisplayGeometry(this);
        public override bool IsValid => true;
        public override string TypeName => "Display Style";
        public override string TypeDescription => "Display style.";
        public override string ToString() => Value?.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            var target = GH_Convert.ToGeometryBase(source);

            if (target == null)
                return false;

            Value = new DisplayGeometry(target, new DisplayMaterial(Color.Black, 0));
            return true;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Material)))
            {
                object ptr = new GH_Material(Value.Material);
                target = (Q)ptr;
                return true;
            }

            return false;
        }

        public override BoundingBox Boundingbox => Value.Geometry.GetBoundingBox(true);

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_DisplayGeometry(Value.Duplicate());
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            return Value.Geometry.GetBoundingBox(xform);
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            return new GH_DisplayGeometry(Value.Transform(xform));
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var copy = Value.Duplicate();
            xmorph.Morph(copy.Geometry);
            return new GH_DisplayGeometry(copy);
        }

        public BoundingBox ClippingBox => Boundingbox;

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (Value.Geometry is Mesh)
            {
                var material = Value.Material ?? args.Material;
                args.Pipeline.DrawMeshShaded(Value.Geometry as Mesh, material);
            }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            //if (Value.Geometry is Mesh)
            //    args.Pipeline.DrawMeshWires(Value.Geometry as Mesh, Value.Color);

            if (Value.Geometry is Curve)
            {
                Color color = Value.Material == null ? args.Color : Value.Material.Diffuse;
                args.Pipeline.DrawCurve(Value.Geometry as Curve, Value.Material.Diffuse);
            }
        }

        bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            obj_guid = Guid.Empty;
            if (Value == null) return false;
            obj_guid = Value.Bake(doc, att);
            return true;
        }

        void IGH_RenderAwareData.AppendRenderGeometry(GH_RenderArgs args, RenderMaterial material)
        {
            if (Value.Geometry is Mesh)
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

                    if (texture != null)
                        mat.SetBitmapTexture(texture);

                    renderMat = mat.RenderMaterial;
                }

                args.Geomety.Add(Value.Geometry as Mesh, renderMat);
            }
        }
    }

    public class GH_MillingAttributes : GH_Goo<MillingAttributes>
    {
        public GH_MillingAttributes() { Value = null; }
        public GH_MillingAttributes(MillingAttributes native) { Value = native; }
        public override IGH_Goo Duplicate() => new GH_MillingAttributes(Value);
        public override bool IsValid => true;
        public override string TypeName => "Milling Attributes";
        public override string TypeDescription => "Milling attributes.";
        public override string ToString() => Value?.ToString();
        public override object ScriptVariable() => Value;
    }

    public class GH_ExtrusionAttributes : GH_Goo<ExtrusionAttributes>
    {
        public GH_ExtrusionAttributes() { Value = null; }
        public GH_ExtrusionAttributes(ExtrusionAttributes native) { Value = native; }
        public override IGH_Goo Duplicate() => new GH_ExtrusionAttributes(Value);
        public override bool IsValid => true;
        public override string TypeName => "Extrusion Attributes";
        public override string TypeDescription => "Extrusion attributes.";
        public override string ToString() => Value?.ToString();
        public override object ScriptVariable() => Value;
    }

    public class DisplayGeometryParameter : GH_PersistentParam<GH_DisplayGeometry>, IGH_PreviewObject, IGH_BakeAwareObject
    {
        public DisplayGeometryParameter() : base("Display Geometry", "DisGeo", "Display geometry.", "Extensions", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.EyeParam;
        public override Guid ComponentGuid => new Guid("{9F90313D-5776-471C-9922-29D4F59A70C4}");

        protected override GH_GetterResult Prompt_Singular(ref GH_DisplayGeometry value)
        {
            value = new GH_DisplayGeometry();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_DisplayGeometry> values)
        {
            values = new List<GH_DisplayGeometry>();
            return GH_GetterResult.success;
        }

        public bool Hidden { get; set; }
        public bool IsPreviewCapable => true;
        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        public void DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
        public void DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);

        public bool IsBakeCapable => true;

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, null, obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            foreach (IGH_BakeAwareData data in VolatileData.AllData(true))
            {
                data.BakeGeometry(doc, att, out Guid id);
                obj_ids.Add(id);
            }
        }
    }

    public class ExtrusionAttributesParameter : GH_PersistentParam<GH_ExtrusionAttributes>
    {
        public ExtrusionAttributesParameter() : base("Extrusion Attributes", "ExtrAtt", "Extrusion attributes.", "Extensions", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.LayersConfigParam;
        public override Guid ComponentGuid => new Guid("{D63464DC-BBAB-4A88-923A-8D9381FB1D0B}");

        protected override GH_GetterResult Prompt_Singular(ref GH_ExtrusionAttributes value)
        {
            value = new GH_ExtrusionAttributes(null);
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_ExtrusionAttributes> values)
        {
            values = new List<GH_ExtrusionAttributes>();
            return GH_GetterResult.success;
        }
    }

    public class MillingAttributesParameter : GH_PersistentParam<GH_MillingAttributes>
    {
        public MillingAttributesParameter() : base("Milling Attributes", "MillAtt", "Milling attributes.", "Extensions", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.LayersConfigParam;
        public override Guid ComponentGuid => new Guid("{21255B77-9E3D-43BD-8FE5-9A77D4A4D575}");
        protected override GH_GetterResult Prompt_Singular(ref GH_MillingAttributes value)
        {
            value = new GH_MillingAttributes();
            return GH_GetterResult.success;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_MillingAttributes> values)
        {
            values = new List<GH_MillingAttributes>();
            return GH_GetterResult.success;
        }
    }
}
