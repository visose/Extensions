using Rhino.Geometry;

namespace Extensions.Grasshopper;

public class VoxelTiles() : Component(
    "Voxel Tiles",
    "VoxelTiles",
    "Places discrete tile geometry on voxels inside a boundary mesh.",
    "Discrete",
    "{769FB3B8-6C88-4130-AA32-FB9D0D1BC6AA}",
    "Cube")
{
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        _ = pManager.AddMeshParameter("Boundary Mesh", "B", "Boundary mesh.", GH_ParamAccess.item);
        _ = pManager.AddNumberParameter("Voxel Size", "S", "Voxel edge length.", GH_ParamAccess.item);
        _ = pManager.AddCurveParameter("Alignment Curves", "A", "Curves used to orient voxels.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Alignment Distance", "Ad", "Maximum distance for weighted curve alignment. Use 0 for closest curve only.", GH_ParamAccess.item);
        _ = pManager.AddGenericParameter("Type Attractors", "D", "Point or curve attractors used to select tile types.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Attractor Distance", "Dd", "Maximum attractor distance for the last tile type.", GH_ParamAccess.item);
        _ = pManager.AddGeometryParameter("Face Types", "Ft", "Tile geometry for face-aligned voxels.", GH_ParamAccess.list);
        _ = pManager.AddGeometryParameter("Edge Types", "Et", "Tile geometry for edge-aligned voxels.", GH_ParamAccess.list);
        _ = pManager.AddGeometryParameter("Corner Types", "Ct", "Tile geometry for corner-aligned voxels.", GH_ParamAccess.list);

        pManager[6].Optional = true;
        pManager[7].Optional = true;
        pManager[8].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        _ = pManager.AddGeometryParameter("Tiles", "G", "Oriented tile geometry.", GH_ParamAccess.list);
        _ = pManager.AddPlaneParameter("Planes", "O", "Voxel orientation planes.", GH_ParamAccess.list);
        _ = pManager.AddIntegerParameter("Types", "T", "Tile type indices.", GH_ParamAccess.list);
        _ = pManager.AddIntegerParameter("Snap Types", "S", "Snap type indices. 0 = face, 1 = edge, 2 = corner.", GH_ParamAccess.list);
        _ = pManager.AddNumberParameter("Distances", "D", "Normalized distances to closest attractors.", GH_ParamAccess.list);
    }

    protected override void SolveComponent(IGH_DataAccess DA)
    {
        var types = new[]
        {
            DA.MaybeList<GeometryBase>(6),
            DA.MaybeList<GeometryBase>(7),
            DA.MaybeList<GeometryBase>(8)
        };
        var typesCount = types.Select(static t => t.Length).ToArray();
        var totalTypes = typesCount.Sum();
        ArgumentOutOfRangeException.ThrowIfZero(totalTypes, nameof(types));

        var voxelSize = DA.Get<double>(1);
        var voxels = Discrete.VoxelTiles.Create(
            DA.Get<Mesh>(0),
            voxelSize,
            [.. DA.List<Curve>(2)],
            DA.Get<double>(3),
            DA.List<object>(4),
            DA.Get<double>(5),
            typesCount);

        List<GeometryBase> geometry = new(voxels.Count);

        foreach (var voxel in voxels)
        {
            var source = types[voxel.SnapType][voxel.Type];
            var placedType = source.Duplicate();
            var xform = Transform.PlaneToPlane(Plane.WorldXY, voxel.Location) * Transform.Scale(Point3d.Origin, voxelSize);
            placedType.Transform(xform);
            geometry.Add(placedType);
        }

        DA.SetDataList(0, geometry);
        DA.SetDataList(1, voxels.Select(static v => v.Location));
        DA.SetDataList(2, voxels.Select(static v => v.Type));
        DA.SetDataList(3, voxels.Select(static v => v.SnapType));
        DA.SetDataList(4, voxels.Select(static v => v.AttractorDistance));
    }
}
