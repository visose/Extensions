using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using static Extensions.Model.Util;
using Extensions.Model.Toolpaths.Milling;
using Grasshopper.Kernel.Types;

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
            pManager.AddParameter(new SpeedParameter(), "Plunge speed", "Ps", "Plunge speed.", GH_ParamAccess.item);
            pManager.AddParameter(new SpeedParameter(), "Cut speed", "Cs", "Cut speed.", GH_ParamAccess.item);
            pManager.AddParameter(new ZoneParameter(), "Plunge zone", "Pz", "Plunge speed.", GH_ParamAccess.item);
            pManager.AddParameter(new ZoneParameter(), "Cut zone", "Ez", "Cut zone.", GH_ParamAccess.item);
        }

        void Outputs(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MillingAttributesParameter(), "Milling Attributes", "A", "Milling attributes.", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int inputCount = 10;
            var inputs = new IGH_Goo[inputCount];

            for (int i = 0; i < inputCount; i++)
            {
                if (!DA.GetData(i, ref inputs[i])) return;
            }

            var target = (inputs[0] as GH_Target).Value as JointTarget;

            var endMill = new EndMill()
            {
                Diameter = (inputs[1] as GH_Number).Value,
                Length = (inputs[2] as GH_Number).Value,
            };

            var attributes = new MillingAttributes()
            {
                EndMill = endMill,
                StepOver = (inputs[3] as GH_Number).Value,
                StepDown = (inputs[4] as GH_Number).Value,
                SafeZOffset = (inputs[5] as GH_Number).Value,
                SafeSpeed = target.Speed,
                PlungeSpeed = (inputs[6] as GH_Speed).Value,
                CutSpeed = (inputs[7] as GH_Speed).Value,
                SafeZone = target.Zone,
                PlungeZone = (inputs[8] as GH_Zone).Value,
                CutZone = (inputs[9] as GH_Zone).Value,
                Tool = target.Tool,
                Frame = target.Frame,
                Home = target.Joints
            }.Initialize();

            DA.SetData(0, new GH_MillingAttributes(attributes));
        }
    }
}