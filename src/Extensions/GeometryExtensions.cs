using Rhino.Geometry;
using static Extensions.Util;

namespace Extensions;

public static class GeometryExtensions
{
    public static Polyline ToPolyline(this Curve curve)
    {
        if (curve == null) return null;

        if (!curve.TryGetPolyline(out Polyline polyline))
            throw new Exception("Curve is not a polyline.");

        return polyline;
    }

    public static IEnumerable<Vector3d> GetNormals(this IList<Point3d> pl)
    {
        if (pl.Count < 3)
            throw new ArgumentException("Polyline must have 3 or more vertices.");

        int last = pl.Count - 1;
        bool isClosed = pl[0].DistanceToSquared(pl[last]) < Tol;
        if (isClosed)
            last--;

        Vector3d lastFaceNormal = Vector3d.ZAxis;

        for (int i = 0; i <= last; i++)
        {
            Point3d prev = i == 0 ? pl[last] : pl[i - 1];
            Point3d next = i == last ? pl[0] : pl[i + 1];
            Vector3d va = pl[i] - prev;
            Vector3d vb = next - pl[i];
            va.Unitize();
            vb.Unitize();

            Vector3d tangent = va + vb;
            tangent.Unitize();

            if (!isClosed)
            {
                if (i == 0)
                    tangent = vb;
                else if (i == last)
                    tangent = va;
            }

            Vector3d faceNormal = Vector3d.CrossProduct(-va, vb);

            if (!isClosed)
            {
                if (i == 0)
                {
                    faceNormal = Vector3d.CrossProduct(pl[0] - pl[1], pl[2] - pl[1]);
                    if (faceNormal.IsTiny()) faceNormal = Vector3d.ZAxis;
                }
                else if (i == last)
                {
                    faceNormal = Vector3d.CrossProduct(pl[last - 2] - pl[last - 1], pl[last] - pl[last - 1]);
                }
            }

            if (i == 0)
                lastFaceNormal = faceNormal;

            if (faceNormal.IsTiny())
                faceNormal = lastFaceNormal;
            else
                lastFaceNormal = faceNormal;

            var normal = Vector3d.CrossProduct(tangent, -faceNormal);
            normal.Unitize();

            if (normal.IsTiny())
                throw new ArgumentException(" Normal invalid.");

            yield return normal;
        }
    }

    public static Point3d ClosestPointFast(this Polyline pl, Point3d testPoint)
    {
        int count = pl.Count;
        if (count < 2) return Point3d.Unset;

        int s_min = 0;
        double t_min = 0.0;
        double d_min = double.MaxValue;

        for (int i = 0; i < count - 1; i++)
        {
            Line seg = new(pl[i], pl[i + 1]);
            double d;
            double t;

            if (seg.Direction.IsTiny(1e-32))
            {
                t = 0.0;
                d = pl[i].DistanceToSquared(testPoint);
            }
            else
            {
                t = seg.ClosestParameter(testPoint);
                if (t <= 0.0) { t = 0.0; }
                else if (t > 1.0) { t = 1.0; }
                d = seg.PointAt(t).DistanceToSquared(testPoint);
            }

            if (d < d_min)
            {
                d_min = d;
                t_min = t;
                s_min = i;
            }
        }

        double pt = s_min + t_min;
        return pl.PointAt(pt);
    }

    public static Mesh FlipYZ(this Mesh mesh)
    {
        var vertices = mesh.Vertices.Select(v => new Point3d(v.X, v.Z, -v.Y)).ToList();
        var normals = mesh.Normals.Select(v => new Vector3f(v.X, v.Z, -v.Y)).ToArray();
        var uv = mesh.TextureCoordinates.ToArray();
        var faces = mesh.Faces;

        var result = new Mesh();
        result.Vertices.AddVertices(vertices);
        result.Normals.AddRange(normals);
        result.Faces.AddFaces(faces);
        result.TextureCoordinates.AddRange(uv);
        return result;
    }
}
