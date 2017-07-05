using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Extensions;


namespace Extensions.View
{
    public class BitmapFromSolidColoredMeshes : GH_Component
    {
        public BitmapFromSolidColoredMeshes() : base("Bitmap from meshes", "BitmapMeshes", "Bitmap from solid colored meshes", "Extensions", "Rendering") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.PaintBrush02;
        public override Guid ComponentGuid => new Guid("{db4d6bc7-9e3c-459b-8494-6fd92dc526ca}");


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "One or more meshes, each with a single vertex color.", GH_ParamAccess.list);
            pManager.AddTextParameter("File path", "F", "File path to a png image.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "M", "Meshes with all uv coords mapped to a single pixel of an image.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var meshes = new List<Mesh>();
            string file = string.Empty;
            DA.GetDataList(0, meshes);
            DA.GetData(1, ref file);

            var outMeshes = RenderExtensions.BitmapFromSolidColoredMeshes(meshes, file);

            DA.SetDataList(0, outMeshes);
        }
    }
}
