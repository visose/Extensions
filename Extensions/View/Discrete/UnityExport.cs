using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Extensions.Model.Discrete;

namespace Extensions.View
{
    public class UnityExport : GH_Component
    {
        public UnityExport() : base("Unity export", "UnityExport", "Exports block instances to Unity 3D.", "Extensions", "Discrete") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Puzzle;
        public override Guid ComponentGuid => new Guid("{09694580-A4BB-4CD8-B061-E158BC83478F}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Block names", "B", "Block names.", GH_ParamAccess.list);
            pManager.AddTextParameter("Instances layer", "I", "Layer name where the instances are placed.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle limit", "Ja", "Maximum angle rotation for joints (0 for rigid joints).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Break force", "Jf", "Break force (in newtons) for joints.", GH_ParamAccess.item);
            pManager.AddTextParameter("File name", "F", "XML export file name.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var blockNames = new List<string>();
            string instancesLayer = null, fileName = null;
            double angleLimit = 0, breakForce = 0;

            if(!DA.GetDataList(0, blockNames)) return;
            if (!DA.GetData(1, ref instancesLayer)) return;
            if (!DA.GetData(2, ref angleLimit)) return;
            if (!DA.GetData(3, ref breakForce)) return;
            if (!DA.GetData(4, ref fileName)) return;

            Assembly.Export(blockNames, instancesLayer, angleLimit, breakForce, fileName, Rhino.RhinoDoc.ActiveDoc);
        }
    }
}
