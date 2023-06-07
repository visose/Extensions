using Rhino.Geometry;
using static System.Math;

namespace Extensions;

public static class GeometryUtil
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

        var plane = new Plane(position, normal);
        var alignAngle = Vector3d.VectorAngle(plane.XAxis, alignment, plane);
        plane.Rotate(alignAngle, plane.Normal);

        return plane;
    }

    public static Vector3d OrientToMesh(Point3d point, Mesh guide, Mesh surface = null)
    {
        var mp = surface == null ?
            guide.ClosestMeshPoint(point, double.MaxValue) :
            surface.ClosestMeshPoint(point, double.MaxValue);

        return guide.NormalAt(mp);
    }
}
