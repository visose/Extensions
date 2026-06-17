using Rhino.Geometry;
using static System.Math;

namespace Extensions;

static class GeometryUtil
{
    public static Vector3d PolarToVector(double a, double b)
    {
        (b, a) = (a, b);
        var n = Vector3d.Zero;
        n.X = Sin(a) * Cos(b);
        n.Y = Sin(a) * Sin(b);
        n.Z = Cos(a);

        return n;
    }

    public static Plane AlignedPlane(Point3d position, Vector3d normal, Vector3d alignment)
    {
        if (!alignment.IsUnitVector)
        {
            alignment = (Point3d)alignment - position;
            alignment.Unitize();
            alignment.Rotate(Util.HalfPI, normal);
        }

        Plane plane = new(position, normal);
        var alignAngle = Vector3d.VectorAngle(plane.XAxis, alignment, plane);
        plane.Rotate(alignAngle, plane.Normal);

        return plane;
    }

    public static Vector3d OrientToMesh(Point3d point, Mesh guide, Mesh? surface = null)
    {
        var mp = (surface ?? guide).ClosestMeshPoint(point, double.MaxValue)
            ?? throw new ArgumentException("Point could not be projected onto the mesh.", nameof(point));

        return guide.NormalAt(mp);
    }
}
