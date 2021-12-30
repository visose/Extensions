using Extensions.Spatial;
using MoreLinq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using static Extensions.Util;
using static System.Math;

namespace Extensions.Discrete;

public class VoxelTiles
{
    static readonly Vector3d[] _faceNormals;
    static readonly Vector3d[] _edgeNormals;
    static readonly Vector3d[] _cornerNormals;

    static VoxelTiles()
    {
        var box = UnitBox();
        _faceNormals = box.FaceNormals.Select(v => new Vector3d(v)).ToArray();
        _cornerNormals = box.Normals.Select(v => new Vector3d(v)).ToArray();
        _edgeNormals = Enumerable.Range(0, box.TopologyEdges.Count)
                        .Select(i =>
                        {
                            var v = (Vector3d)box.TopologyEdges.EdgeLine(i).PointAt(0.5);
                            v.Unitize();
                            return v;
                        }).ToArray();
    }

    static Mesh UnitBox(double scale = 1.0)
    {
        double l = 0.5 * scale;
        var corner = new Point3d(l, l, l);
        var bbox = new BoundingBox(-corner, corner);
        var meshBox = Mesh.CreateFromBox(bbox, 1, 1, 1);
        meshBox.Weld(PI2);
        meshBox.Vertices.CombineIdentical(true, true);
        meshBox.Compact();
        meshBox.RebuildNormals();

        return meshBox;
    }

    public static IList<Voxel> Create(Mesh boundary, double length, List<Curve> alignments, double alignmentDistance, List<GeometryBase> attractors, double attractorDistance, int[] typesCount)
    {
        var voxels = new VoxelTiles(boundary, length, alignments, alignmentDistance, attractors, attractorDistance, typesCount);
        var result = voxels.GetVoxels().Where(v => v.IsActive);
        return result.ToList();
    }

    internal Voxel[,,] Voxels;
    internal Vector3i Size;
    internal double VoxelSize;
    internal Point3d Corner;
    //readonly int _count;
    readonly int[] _typesCount;

    internal VoxelTiles(Mesh boundary, double length, List<Curve> alignments, double alignmentDistance, List<GeometryBase> attractors, double attractorDistance, int[] typesCount)
    {
        VoxelSize = length;
        _typesCount = typesCount;

        var bbox = boundary.GetBoundingBox(true);

        var sizef = bbox.Diagonal / length;
        Size = new Vector3i((int)sizef.X, (int)sizef.Y, (int)sizef.Z);
        //_count = Size.X * Size.Y * Size.Z;
        sizef = new Vector3d(Size.X, Size.Y, Size.Z);
        Corner = bbox.Min + (bbox.Diagonal - sizef * length) * 0.5f;

        // make voxels
        Voxels = new Voxel[Size.X, Size.Y, Size.Z];

        for (int z = 0; z < Size.Z; z++)
            for (int y = 0; y < Size.Y; y++)
                for (int x = 0; x < Size.X; x++)
                {
                    Voxels[x, y, z] = new Voxel(new Vector3i(x, y, z), this);
                }

        // voxelize shape
        SetActiveVoxels(boundary);

        // type
        SetTypeClosest(attractors, attractorDistance);

        // orientation
        if (alignmentDistance == 0)
            SetAlignmentsClosest(alignments);
        else
            SetAlignmentsFalloff(alignments, alignmentDistance);
    }

    void SetActiveVoxels(Mesh boundary)
    {
        var unitBox = UnitBox(VoxelSize);

        Parallel.ForEach(GetVoxels(), voxel =>
        {
            var p = voxel.Location.Origin;
            if (boundary.IsPointInside(p, Tol, true))
            {
                voxel.IsActive = true;
            }
            else
            {
                var m = unitBox.DuplicateMesh();
                m.Translate((Vector3d)p);
                var clashes = MeshClash.Search(m, boundary, 0, 1);
                voxel.IsActive = clashes.Length > 0;
            }
        });
    }

    void SetTypeClosest(IEnumerable<GeometryBase> attractors, double maxDistance)
    {
        var points = attractors.Where(a => a is Point).Select(p => (p as Point).Location);
        var curves = attractors.Where(a => a is Curve).Select(c => (c as Curve));
        var pointCloud = new PointCloud(points);

        foreach (var voxel in GetVoxels().Where(v => v.IsActive))
        {
            Point3d p = voxel.Location.Origin;
            var closestIndex = pointCloud.ClosestPoint(p);
            var closestPoint = pointCloud[closestIndex].Location;

            double minDistance = p.DistanceToSquared(closestPoint);

            foreach (var curve in curves)
            {
                if (curve.ClosestPoint(p, out double t, minDistance))
                {
                    minDistance = curve.PointAt(t).DistanceToSquared(p);
                }
            }

            var distance = Sqrt(minDistance);

            var param = distance / maxDistance;
            param = Rhino.RhinoMath.Clamp(param, 0.0, 1.0);
            var typeCount = _typesCount.Max();
            var type = (int)(param * typeCount);
            if (type == typeCount) type--;
            voxel.AttractorDistance = param;
            voxel.Type = type;
        }
    }

    void SetAlignmentsClosest(IEnumerable<Curve> curves)
    {
        foreach (var voxel in GetVoxels().Where(v => v.IsActive))
        {
            Point3d p = voxel.Location.Origin;
            double minDistance = double.MaxValue;
            Curve minCurve = null;
            double minT = 0;

            foreach (var curve in curves)
            {
                if (curve.ClosestPoint(p, out double t, minDistance))
                {
                    minDistance = curve.PointAt(t).DistanceToSquared(p);
                    minT = t;
                    minCurve = curve;
                }
            }

            var tangent = minCurve.TangentAt(minT);
            var curvature = minCurve.CurvatureAt(minT);
            var normal = Vector3d.CrossProduct(tangent, curvature);
            (voxel.Location, voxel.SnapType) = SnapPlane(voxel, normal, tangent);
        }
    }

    void SetAlignmentsFalloff(IList<Curve> curves, double maxDistance)
    {
        foreach (var voxel in GetVoxels().Where(v => v.IsActive))
        {
            Point3d p = voxel.Location.Origin;

            double sumWeight = 0;
            var tangent = Vector3d.Zero;
            var curvature = Vector3d.Zero;

            foreach (var curve in curves)
            {
                if (curve.ClosestPoint(p, out double t, maxDistance))
                {
                    var weight = maxDistance - curve.PointAt(t).DistanceTo(p);
                    tangent += curve.TangentAt(t) * weight;
                    curvature += curve.CurvatureAt(t) * weight;
                    sumWeight += weight;
                }

                tangent /= sumWeight;
                curvature /= sumWeight;
            }

            if (sumWeight < UnitTol)
            {
                voxel.IsActive = false;
            }
            else
            {
                var normal = Vector3d.CrossProduct(tangent, curvature);
                var (plane, snapType) = SnapPlane(voxel, normal, tangent);
                voxel.Location = plane;
                voxel.SnapType = snapType;
            }
        }
    }

    (Plane plane, int snapType) SnapPlane(Voxel voxel, Vector3d normal, Vector3d xAxis)
    {
        var snapVectors = new Vector3d[][]
            {
                   _faceNormals,
                   _edgeNormals,
                   _cornerNormals
            };

        var snaps = new List<(Vector3d vector, double distance, int snapType)>();

        for (int i = 0; i < 3; i++)
        {
            if (_typesCount[i] - 1 >= voxel.Type)
            {
                var (vector, distance) = VectorSnap(xAxis, snapVectors[i]);
                snaps.Add((vector, distance, i));
            }
        }

        var xSnap = snaps.MinBy(s => s.distance).First();

        var subVectors = snapVectors[xSnap.snapType]
                            .Where(v => Abs(Vector3d.VectorAngle(xSnap.vector, v) - HalfPI) < HalfPI * 0.25);

        var nSnap = VectorSnap(normal, subVectors);
        var ySnap = -Vector3d.CrossProduct(nSnap.vector, xSnap.vector);

        return (new Plane(voxel.Location.Origin, xSnap.vector, ySnap), xSnap.snapType);
    }

    (Vector3d vector, double distance) VectorSnap(Vector3d vector, IEnumerable<Vector3d> snapVectors)
    {
        double minAngle = double.MaxValue;
        Vector3d minVector = Vector3d.Unset;

        foreach (var v in snapVectors)
        {
            var angle = Vector3d.VectorAngle(vector, v);
            if (angle < minAngle)
            {
                minAngle = angle;
                minVector = v;
            }
        }

        return (minVector, minAngle);
    }

    IEnumerable<Voxel> GetVoxels()
    {
        for (int z = 0; z < Size.Z; z++)
            for (int y = 0; y < Size.Y; y++)
                for (int x = 0; x < Size.X; x++)
                {
                    yield return Voxels[x, y, z];
                }
    }
}

public class Voxel
{
    public Vector3i Index;
    public Plane Location;
    public bool IsActive;
    public int Type;
    public double AttractorDistance;
    public int SnapType;

    //public GeometryBase Geometry;


    public Voxel(Vector3i index, VoxelTiles grid)
    {
        Index = index;
        Location = Plane.WorldXY;
        Location.Origin = grid.Corner + new Vector3d(index.X + 0.5f, index.Y + 0.5f, index.Z + 0.5f) * grid.VoxelSize;
    }
}
