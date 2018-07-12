using System;
using System.Drawing;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Extensions.Model.Document;
using Rhino.Geometry;

namespace Extensions.View
{
    public class GH_DisplayStyle : GH_Goo<DisplayStyle>, IGH_PreviewData
    {
        public GH_DisplayStyle() { Value = new DisplayStyle(null, Color.Black); }
        public GH_DisplayStyle(GH_DisplayStyle goo) { Value = goo.Value; }
        public GH_DisplayStyle(DisplayStyle native) { Value = native; }
        public override IGH_Goo Duplicate() => new GH_DisplayStyle(this);
        public override bool IsValid => true;
        public override string TypeName => "DisplayStyle";
        public override string TypeDescription => "Display style";
        public override string ToString() => Value?.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is GeometryBase)
            {
                Value = new DisplayStyle(source as GeometryBase, Color.Black);
                return true;
            }

            if (source is GH_Point)
            {
                var point = new Rhino.Geometry.Point((source as GH_Point).Value);
                Value = new DisplayStyle(point, Color.Black);
                return true;
            }

            return false;
        }

        public BoundingBox ClippingBox => Value.Geometry.GetBoundingBox(true);

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (Value.Geometry is Mesh)
                args.Pipeline.DrawMeshShaded(Value.Geometry as Mesh, args.Material);
        }


        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            //if (Value.Geometry is Mesh)
            //    args.Pipeline.DrawMeshWires(Value.Geometry as Mesh, Value.Color);

            if (Value.Geometry is Curve)
                args.Pipeline.DrawCurve(Value.Geometry as Curve, Value.Color);
        }

    }

    public class DisplayStyleParameter : GH_PersistentParam<GH_DisplayStyle>, IGH_PreviewObject
    {
        public DisplayStyleParameter() : base("Display style", "Style", "Display style.", "Extensions", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.EyeParam;
        public override Guid ComponentGuid => new Guid("{9F90313D-5776-471C-9922-29D4F59A70C4}");
        protected override GH_GetterResult Prompt_Singular(ref GH_DisplayStyle value)
        {
            value = new GH_DisplayStyle();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_DisplayStyle> values)
        {
            values = new List<GH_DisplayStyle>();
            return GH_GetterResult.success;
        }

        public bool Hidden { get; set; }
        public bool IsPreviewCapable => true;
        public BoundingBox ClippingBox => base.Preview_ComputeClippingBox();
        public void DrawViewportWires(IGH_PreviewArgs args) => base.Preview_DrawWires(args);
        public void DrawViewportMeshes(IGH_PreviewArgs args) => base.Preview_DrawMeshes(args);
    }
}
