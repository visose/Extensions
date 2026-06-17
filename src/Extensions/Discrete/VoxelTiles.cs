using Extensions.Spatial;
using MoreLinq;
using Rhino;
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
        Point3d corner = new(l, l, l);
        BoundingBox bbox = new(-corner, corner);
        var meshBox = Mesh.CreateFromBox(bbox, 1, 1, 1)
            ?? throw new InvalidOperationException("Could not create unit box mesh.");

        meshBox.Weld(PI2);
        meshBox.Vertices.CombineIdentical(true, true);
        meshBox.Compact();
        meshBox.RebuildNormals();

        return meshBox;
    }

    public static IReadOnlyList<Voxel> Create(Mesh boundary, double length, IReadOnlyList<Curve> alignments, double alignmentDistance, IReadOnlyList<object> attractors, double attractorDistance, IReadOnlyList<int> typesCount)
    {
        VoxelTiles voxels = new(boundary, length, alignments, alignmentDistance, attractors, attractorDistance, typesCount);
        var result = voxels.GetVoxels().Where(v => v.IsActive);
        return result.ToList();
    }

    internal Voxel[,,] Voxels;
    internal Vector3i Size;
    internal double VoxelSize;
    internal Point3d Corner;
    readonly IReadOnlyList<int> _typesCount;

    internal VoxelTiles(Mesh boundary, double length, IReadOnlyList<Curve> alignments, double alignmentDistance, IReadOnlyList<object> attractors, double attractorDistance, IReadOnlyList<int> typesCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length, nameof(length));
        ArgumentOutOfRangeException.ThrowIfNegative(alignmentDistance, nameof(alignmentDistance));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attractorDistance, nameof(attractorDistance));
        ArgumentOutOfRangeException.ThrowIfNotEqual(typesCount.Count, 3, nameof(typesCount));

        VoxelSize = length;
        _typesCount = typesCount;

        var bbox = boundary.GetBoundingBox(true);

        var sizef = bbox.Diagonal / length;
        Size = new((int)sizef.X, (int)sizef.Y, (int)sizef.Z);
        sizef = new(Size.X, Size.Y, Size.Z);
        Corner = bbox.Min + (bbox.Diagonal - sizef * length) * 0.5f;

        Voxels = new Voxel[Size.X, Size.Y, Size.Z];

        for (int z = 0; z < Size.Z; z++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    Voxels[x, y, z] = new(new(x, y, z), this);
                }
            }
        }

        SetActiveVoxels(boundary);
        SetTypeClosest(attractors, attractorDistance);

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

    void SetTypeClosest(IEnumerable<object> attractors, double maxDistance)
    {
        List<Point3d> points = [];
        List<Curve> curves = [];

        foreach (var attractor in attractors)
        {
            switch (attractor)
            {
                case null:
                    throw new ArgumentException("Attractor list contains a null value.", nameof(attractors));
                case Point3d point:
                    points.Add(point);
                    break;
                case Point point:
                    points.Add(point.Location);
                    break;
                case Curve curve:
                    curves.Add(curve);
                    break;
                default:
                    throw new ArgumentException($"Attractor type '{attractor.GetType().Name}' is not supported.", nameof(attractors));
            }
        }

        PointCloud? pointCloud = points.Count > 0 ? new(points) : null;

        foreach (var voxel in GetVoxels().Where(v => v.IsActive))
        {
            Point3d p = voxel.Location.Origin;
            double minDistance = double.MaxValue;

            if (pointCloud is not null)
            {
                var closestIndex = pointCloud.ClosestPoint(p);
                var closestPoint = pointCloud[closestIndex].Location;
                minDistance = p.DistanceToSquared(closestPoint);
            }

            foreach (var curve in curves)
            {
                if (curve.ClosestPoint(p, out double t, minDistance))
                {
                    minDistance = curve.PointAt(t).DistanceToSquared(p);
                }
            }

            if (double.IsPositiveInfinity(minDistance) || minDistance == double.MaxValue)
                throw new ArgumentException("At least one point or curve attractor is required.", nameof(attractors));

            var distance = Sqrt(minDistance);

            var param = distance / maxDistance;
            param = RhinoMath.Clamp(param, 0.0, 1.0);
            var typeCount = _typesCount.Max();
            var type = (int)(param * typeCount);

            if (type == typeCount)
                type--;

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
            Curve? minCurve = null;
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

            if (minCurve is null)
            {
                voxel.IsActive = false;
                continue;
            }

            var tangent = minCurve.TangentAt(minT);
            var curvature = minCurve.CurvatureAt(minT);
            var normal = Vector3d.CrossProduct(tangent, curvature);
            (voxel.Location, voxel.SnapType) = SnapPlane(voxel, normal, tangent);
        }
    }

    void SetAlignmentsFalloff(IReadOnlyList<Curve> curves, double maxDistance)
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

            }

            if (sumWeight < UnitTol)
            {
                voxel.IsActive = false;
            }
            else
            {
                tangent /= sumWeight;
                curvature /= sumWeight;

                var normal = Vector3d.CrossProduct(tangent, curvature);
                var (plane, snapType) = SnapPlane(voxel, normal, tangent);
                voxel.Location = plane;
                voxel.SnapType = snapType;
            }
        }
    }

    (Plane plane, int snapType) SnapPlane(Voxel voxel, Vector3d normal, Vector3d xAxis)
    {
        Vector3d[][] snapVectors =
        [
            _faceNormals,
            _edgeNormals,
            _cornerNormals
        ];

        List<(Vector3d vector, double distance, int snapType)> snaps = [];

        for (int i = 0; i < 3; i++)
        {
            if (_typesCount[i] - 1 >= voxel.Type)
            {
                var (vector, distance) = VectorSnap(xAxis, snapVectors[i]);
                snaps.Add((vector, distance, i));
            }
        }

        var xSnap = snaps.Minima(s => s.distance).First();

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
        {
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    yield return Voxels[x, y, z];
                }
            }
        }
    }
}

public class Voxel
{
    internal Vector3i Index { get; }

    public Plane Location { get; internal set; }
    public bool IsActive { get; internal set; }
    public int Type { get; internal set; }
    public double AttractorDistance { get; internal set; }
    public int SnapType { get; internal set; }
    internal Voxel(Vector3i index, VoxelTiles grid)
    {
        Index = index;
        var location = Plane.WorldXY;
        Vector3d offset = new(index.X + 0.5f, index.Y + 0.5f, index.Z + 0.5f);
        location.Origin = grid.Corner + offset * grid.VoxelSize;
        Location = location;
    }
}
