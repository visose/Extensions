﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace Extensions.Grasshopper;

public class DifferentialGrowth : GH_Component
{
    public DifferentialGrowth() : base("Differential Growth", "DiffGrowth", "Grows a polyline using a differential growth algorithm.", "Extensions", "Geometry") { }
    protected override System.Drawing.Bitmap Icon => Util.GetIcon("Virus");
    public override Guid ComponentGuid => new("{64C4B469-E923-4B7E-B746-C2599F7ED0A0}");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGeometryParameter("Region", "R", "Boundary region as either a planar polyline or a mesh.", GH_ParamAccess.item);
        pManager.AddCurveParameter("Polylines", "P", "Polylines to grow.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Radius", "R", "Collision radius.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Iterations", "I", "Simulation iterations.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Convergence", "C", "Convergence iterations.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Polylines", "P", "Resulting polylines.", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GeometryBase region = null;
        var curves = new List<Curve>();
        double radius = 0;
        int iterations = 0, convergence = 0;

        DA.GetData(0, ref region);
        DA.GetDataList(1, curves);
        DA.GetData(2, ref radius);
        DA.GetData(3, ref iterations);
        DA.GetData(4, ref convergence);

        Polyline polyline = null;
        Mesh mesh = null;

        if (region is Curve)
        {
            polyline = (region as Curve).ToPolyline();
        }
        else
        {
            mesh = region is Mesh
                ? region as Mesh
                : throw new Exception(" Region should be polyline or mesh.");
        }

        var inPolylines = curves.Select(c => c.ToPolyline()).ToList();

        var simulation = new Simulations.DifferentialGrowth.DifferentialGrowth(inPolylines, radius, convergence, iterations, polyline, mesh);
        var polylines = simulation.AllPolylines;

        var outCurves = new GH_Structure<GH_Curve>();
        var path = DA.ParameterTargetPath(0);

        for (int j = 0; j < polylines.Count; j++)
        {
            outCurves.AppendRange(polylines[j].Select(p => new GH_Curve(p.ToNurbsCurve())), path.AppendElement(j));
        }

        DA.SetDataTree(0, outCurves);
    }
}
