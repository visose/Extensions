using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using static Extensions.Model.Util;
using Extensions.Model.Toolpaths.Milling;

namespace Extensions.View
{
    public class CreateMillingAttributes : GH_Component
    {
        public CreateMillingAttributes() : base("Milling Attributes", "MillAtt", "Milling attributes.", "Extensions", "Toolpaths") { }
        public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.LayersConfig;
        public override Guid ComponentGuid => new Guid("{F6A3EFA3-2CC5-4E17-BEE8-E8B9AA6648B5}");

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
            pManager.AddParameter(new TargetParameter(), "Reference target", "T", "Create targets will inherit the tool and frame from this target.", GH_ParamAccess.item);
            pManager.AddNumberParameter("End mill diameter", "D", "End mill diameter.", GH_ParamAccess.item);
            pManager.AddNumberParameter("End mill length", "L", "End mill length.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step over", "So", "Step over in mm.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step down", "Sd", "Step down in mm.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Safe Z offset", "Z", "Vertical margin to add to plunges.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Safe speed", "Ss", "Safe speed in mm/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Plunge speed", "Ps", "Plunge speed in mm/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Cut speed", "Cs", "Cut speed in mm/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Cut zone", "Cz", "Cut zone in mm.", GH_ParamAccess.item);
        }

        void Outputs(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MillingAttributesParameter(), "Milling Attributes", "A", "Milling attributes.", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Target target = null;
            var values = new double[9];

            if (!DA.GetData(0, ref target)) return;

            for (int i = 0; i < 9; i++)
            if (!DA.GetData(i+1, ref values[i])) return;


            var endMill = new EndMill()
            {
                Diameter = values[0],
                Length = values[1]
            };

            var attributes = new MillingAttributes(target.Value, endMill, values[2], values[3], values[4], values[5], values[6], values[7],values[8]);

            DA.SetData(0, new GH_MillingAttributes(attributes));
        }
    }
}