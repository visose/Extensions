using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Extensions;
using Extensions.Model.Document;
using Rhino.Display;

namespace Extensions.View
{
    public class BitMapFromVertexColors : GH_Component
    {
        public BitMapFromVertexColors() : base("Bitmap From Mesh", "BmpMesh", "Creates a bitmap from a mesh with multiple vertex colors and assigns the corresponding uv coordinates to the mesh.", "Extensions", "Rendering") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.PaintBrush01;
        public override Guid ComponentGuid => new Guid("{7aedf2f4-75e2-48be-94c5-fe116caf8b26}");


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Single mesh with render colors.", GH_ParamAccess.item);
            pManager.AddTextParameter("File path", "F", "File path to a PNG image.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new DisplayGeometryParameter(), "Dispay mesh", "M", "Display geometry object with texture coords mapped to the bitmap.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            string file = string.Empty;
            DA.GetData(0, ref mesh);
            DA.GetData(1, ref file);

            Mesh outMesh = RenderExtensions.BitmapFromVertexColors(mesh, file);
            var material = new DisplayMaterial();
            material.SetBitmapTexture(file, true);

            var display = new DisplayGeometry(outMesh, material);
            DA.SetData(0, new GH_DisplayGeometry(display));
        }
    }
}
