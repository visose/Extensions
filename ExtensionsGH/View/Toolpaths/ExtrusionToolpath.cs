using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using static Extensions.Model.Util;
using Extensions.Model.Toolpaths.Extrusion;

namespace Extensions.View
{
    public class CreateExternalExtrusionToolpath : GH_Component
    {
        public CreateExternalExtrusionToolpath() : base("Extrusion Toolpath Ex", "ExtPath", "Extrusion toolpath with external axis.", "Extensions", "Toolpaths") { }
        public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.LayersAdd;
        public override Guid ComponentGuid => new Guid("{08731061-8020-4204-8B69-198AC90BCE5E}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            if (ExtensionsInfo.IsRobotsInstalled)
                Inputs(pManager);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            if (ExtensionsInfo.IsRobotsInstalled)
                Outputs(pManager);
        }

        void Inputs(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Paths", "P", "Paths as as list of polylines.", GH_ParamAccess.list);
            pManager.AddParameter(new ExtrusionAttributesParameter(), "Extrusion attributes", "A", "Extrusion attributes.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Extrusion Factor", "F", "Extrusion factor.", GH_ParamAccess.item, 0.21);
            pManager.AddNumberParameter("Suck Back", "Sb", "Distance to extrude in reverse immediately after stopping the extrusion. To avoid dripping material.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Start Distance", "Sd", "Distance to extrude before the robot starts moving. To compensate for material not flowing immediately after starting the extrusion.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Test Loop", "L", "Distance for test loop.", GH_ParamAccess.item, 200);
        }

        void Outputs(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new ToolpathParameter(), "Toolpath", "T", "Extrusion toolpath.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Layer indices", "L", "Layer indices.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var paths = new List<Curve>();
            GH_ExtrusionAttributes attributes = null;
            double factor = 0, suckBack = 0, loop = 0, startDistance = 0;

            if (!DA.GetDataList(0, paths)) return;
            if (!DA.GetData(1, ref attributes)) return;
            if (!DA.GetData(2, ref factor)) return;
            if (!DA.GetData(3, ref suckBack)) return;
            if (!DA.GetData(4, ref startDistance)) return;
            if (!DA.GetData(5, ref loop)) return;

            var polylines = paths
                .Where(p => p.IsPolyline())
                .Select(p => { p.TryGetPolyline(out Polyline pl); return pl; })
                .ToList();

            var toolpath = new ExternalExtrusionToolpath(polylines, attributes.Value, factor, suckBack, startDistance, loop);

            DA.SetData(0, toolpath);
            DA.SetDataList(1, toolpath.SubPrograms);
        }
    }
}