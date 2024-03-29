﻿using Grasshopper.Kernel;
using Extensions.Discrete;

namespace Extensions.Grasshopper;

public class UnityExport : GH_Component
{
    public UnityExport() : base("Unity Export", "UnityExport", "Exports block instances to Unity 3D.", "Extensions", "Discrete") { }
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("Puzzle");
    public override Guid ComponentGuid => new("{09694580-A4BB-4CD8-B061-E158BC83478F}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Block names", "B", "Block names.", GH_ParamAccess.list);
        pManager.AddTextParameter("Instances layer", "I", "Layer name where the instances are placed.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Density", "D", "Density of the tiles (weight/volume).", GH_ParamAccess.item);
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
        double density = 0, angleLimit = 0, breakForce = 0;

        if (!DA.GetDataList(0, blockNames)) return;
        if (!DA.GetData(1, ref instancesLayer)) return;
        if (!DA.GetData(2, ref density)) return;
        if (!DA.GetData(3, ref angleLimit)) return;
        if (!DA.GetData(4, ref breakForce)) return;
        if (!DA.GetData(5, ref fileName)) return;

        Assembly.Export(blockNames, instancesLayer, density, angleLimit, breakForce, fileName, Rhino.RhinoDoc.ActiveDoc);
    }
}
