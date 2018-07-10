using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using Extensions.Model.Document;
using System.Collections.Generic;

namespace Extensions.View
{
    public class CreateDisplayStyle : GH_Component
    {
        public CreateDisplayStyle() : base("Display style", "DisplayStyle", "Attach display attributes to geometry.", "Extensions", "Document") { }
        protected override Bitmap Icon => Properties.Resources.Eye;
        public override Guid ComponentGuid => new Guid("{07955694-55A9-4AC6-88B8-A0F37632634B}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Geometry.", GH_ParamAccess.item);
            pManager.AddColourParameter("Color", "C", "Display color.", GH_ParamAccess.item, Color.Black);
            pManager.AddTextParameter("Layer", "L", "Layer name.", GH_ParamAccess.item, "Default");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new DisplayStyleParameter(), "Display style", "D", "Display style.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeometryBase geometry = null;
            Color color = Color.Black;
            string layer = "";

            if (!DA.GetData(0, ref geometry)) return;
            if (!DA.GetData(1, ref color)) return;
            if (!DA.GetData(2, ref layer)) return;

            var displayStyle = new DisplayStyle(geometry, color, layer);

            DA.SetData(0, new GH_DisplayStyle(displayStyle));
        }
    }
}
