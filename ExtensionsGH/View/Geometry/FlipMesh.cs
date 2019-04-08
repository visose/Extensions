using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Extensions;


namespace Extensions.View
{
    public class FlipMesh : GH_Component
    {
        public FlipMesh() : base("Flip mesh", "MeshFlip", "Flips the direction of a mesh.", "Extensions", "Geometry") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Undo;
        public override Guid ComponentGuid => new Guid("{65433478-f2d7-4cd0-808f-b1d1834270c3}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh to flip.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Flipped mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            DA.GetData(0, ref mesh);

            Mesh outMesh = mesh.DuplicateMesh();
            outMesh.Flip(true, true, true);

            DA.SetData(0, outMesh);
        }
    }
}
