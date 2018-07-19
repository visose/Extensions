using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Robots;
using Robots.Grasshopper;
using static Extensions.Model.Util;

namespace Extensions.View
{
    public class CSVConverter : GH_Component
    {
        public CSVConverter() : base("CSV Toolpath", "CSVPath", "Converts a CSV file to robot targets.", "Extensions", "Toolpaths") { }
        public override GH_Exposure Exposure => ExtensionsInfo.IsRobotsInstalled ? GH_Exposure.primary : GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Fingerprint;
        public override Guid ComponentGuid => new Guid("{0C7F5A9E-40CC-4A87-AB23-6333D274FF14}");

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
            pManager.AddTextParameter("File", "F", "Text file. Each target should be one line, values should be separated by commas.", GH_ParamAccess.item);
            pManager.AddParameter(new TargetParameter(), "Target", "T", "Created targets will use the parameters of this reference target if they're not supplied in the CSV file.", GH_ParamAccess.item);
            pManager.AddTextParameter("Mask", "M", "Mask representing CSV format. Parameters allowed: 'position, normal, xaxis, speed, zone, type'. The first three should be 3 values (x,y,z). Distance values should be defined in mm. Speed is defined in mm/minute. Type should be an integer (0,1) where 0 is a rapid move and 1 is a cutting move.", GH_ParamAccess.item, "position, normal, speed");
            pManager.AddBooleanParameter("Reverse normal", "R", "Reverses the tool normal.", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Cutting speed", "C", "Maximum speed in mm/min that will be considered a cutting move.", GH_ParamAccess.item, 2750);
            pManager.AddPointParameter("Point alignment", "P", "Aligns the X axis of the tagets to look towards this point (in world coordinate system). If not supplied it will use the X axis of the reference target.", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        void Outputs(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TargetParameter(), "Targets", "T", "List of targets.", GH_ParamAccess.list);
            pManager.AddCurveParameter("ToolPath", "C", "Only the cutting paths.", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string file = null, mask = null;
            GH_Target target = null;
            bool reverse = false;
            double cuttingSpeed = 0;
            Point3d? point = null;

            if (!DA.GetData(0, ref file)) return;
            if (!DA.GetData(1, ref target)) return;
            if (!DA.GetData(2, ref mask)) return;
            if (!DA.GetData(3, ref reverse)) return;
            DA.GetData(4, ref cuttingSpeed);
            DA.GetData(5, ref point);

            var converter = new Extensions.Model.Toolpaths.CSVConverter(file, target.Value as CartesianTarget, mask, reverse, cuttingSpeed, point);

            DA.SetDataList(0, converter.Targets);
            DA.SetDataList(1, converter.ToolPath);
        }
    }
}