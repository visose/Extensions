using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Extensions;
using static Extensions.Model.Util;

namespace Extensions.View
{
    public class StraightSkeleton : GH_Component
    {
        public StraightSkeleton() : base("Straight Skeleton", "StrSkel", "Returns the straight skeleton of a polygon.", "Extensions", "Geometry") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Graph;
        public override Guid ComponentGuid => new Guid("{d529efd9-2fdd-4751-a6b4-307c8f82390b}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polygon", "P", "Polygon to create the straight skeleton.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Regions", "R", "Closed regions of the straight skeleton.", GH_ParamAccess.list);
          //  pManager.AddCurveParameter("Axis", "A", "Attempt to get a medial axis.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            DA.GetData(0, ref curve);
            Polyline polyline = curve.ToPolyline();

            var regions = Model.StraightSkeleton.StraightSkeleton.GetStraightSkeleton(polyline);
            var regionCurves = regions.Select(e => e.ToNurbsCurve());
           // var axis = Model.StraightSkeleton.StraightSkeleton.GetAxis(regions, polyline);

            DA.SetDataList(0, regionCurves);
            //DA.SetData(1, new PolylineCurve(axis));
        }
    }
}
