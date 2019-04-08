using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace Extensions.View
{
    public class VoxelTiles : GH_Component
    {
        public VoxelTiles() : base("Voxel Tiles", "VoxelTiles", "Voxelizes a boundary shape and places discrete elements on each voxel based on guide curves and points.", "Extensions", "Discrete") { }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Cube;
        public override Guid ComponentGuid => new Guid("{769FB3B8-6C88-4130-AA32-FB9D0D1BC6AA}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Boundary mesh", "B", "Boundary mesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Voxel size", "S", "Length of the voxel edges.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curves for alignment", "A", "Curves for alignment.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Alignment max distance", "Ad", "The weighted average direction of curves within this distance will be used. If no curves are within this distance the voxel is removed. Use 0 for closest curve only.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Attractors for types", "D", "Types are selected based on the distance to this geometry. Only points and curves are supported.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Attractors max distance", "Dd", "Max distance for the last type.", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Face types", "Ft", "Face types. Only one geometry object per type. If a type consists on multiple objects, use a group object.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Edge types", "Et", "Edge types.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Corner types", "Ct", "Corner types.", GH_ParamAccess.list);

            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Voxel geometry", "G", "Oriented tiles.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Voxel orientation", "O", "Orientation of each voxel as a plane.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Voxel type", "T", "Voxel type as index values.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Voxel snap type", "S", "Voxel snap types. 0 = face, 1 = edge, 2 = corner.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Voxel distance", "D", "Normalized distance to closest attractor.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh boundary = null;
            double length = 0;
            var alignments = new List<Curve>();
            var ghAttractors = new List<IGH_GeometricGoo>();
            double alignmentDistance = 0, attractorDistance = 0;
            var faceTypes = new List<IGH_GeometricGoo>();
            var edgeTypes = new List<IGH_GeometricGoo>();
            var cornerTypes = new List<IGH_GeometricGoo>();

            if (!DA.GetData(0, ref boundary)) return;
            if (!DA.GetData(1, ref length)) return;
            if (!DA.GetDataList(2, alignments)) return;
            if (!DA.GetData(3, ref alignmentDistance)) return;
            if (!DA.GetDataList(4, ghAttractors)) return;
            if (!DA.GetData(5, ref attractorDistance)) return;
            DA.GetDataList(6, faceTypes);
            DA.GetDataList(7, edgeTypes);
            DA.GetDataList(8, cornerTypes);

            var attractors = new List<GeometryBase>();

            foreach (var geo in ghAttractors)
            {
                if (geo is GH_Point)
                {
                    var point = geo as GH_Point;
                    attractors.Add(new Point(point.Value));
                }
                else if (geo is GH_Curve)
                {
                    var curve = geo as GH_Curve;
                    attractors.Add(curve.Value);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Geometry '{geo.TypeDescription}' not supported as attractor.");
                }
            }

            var types = new[]
            {
                faceTypes,
                edgeTypes,
                cornerTypes
            };

            var typesCount = types.Select(t => t.Count).ToArray();

            if (typesCount.Sum() == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to input at least one type.");
                return;
            }

            var voxels = Model.Discrete.VoxelTiles.Create(boundary, length, alignments, alignmentDistance, attractors, attractorDistance, typesCount);

            var geometry = new List<IGH_GeometricGoo>();

            foreach (var voxel in voxels)
            {
                var type = types[voxel.SnapType][voxel.Type];
                var xform = Transform.PlaneToPlane(Plane.WorldXY, voxel.Location) * Transform.Scale(Point3d.Origin, length);
                var placedType = type.DuplicateGeometry();
                placedType = placedType.Transform(xform);
                geometry.Add(placedType);
            }

            var orientations = voxels.Select(v => v.Location);
            var voxelTypes = voxels.Select(v => v.Type);
            var snapTypes = voxels.Select(v => v.SnapType);
            var voxelDistances = voxels.Select(v => v.AttractorDistance);

            DA.SetDataList(0, geometry);
            DA.SetDataList(1, orientations);
            DA.SetDataList(2, voxelTypes);
            DA.SetDataList(3, snapTypes);
            DA.SetDataList(4, voxelDistances);
        }
    }
}
