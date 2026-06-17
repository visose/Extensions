using Rhino.Geometry;
using static System.Math;
using static Extensions.Util;

namespace Extensions.Geometry;

static class MeshPipe
{
    public static Mesh MeshFlatPolyline(Polyline polyline, double width, double height, double fillet = 0, int segments = 24)
    {
        if (fillet > 0)
        {
            polyline = Fillet(polyline, fillet);
        }

        return MeshExtrusion(polyline, width, height, segments);
    }

    public static Mesh MeshExtrusion(Polyline polyline, double width, double height, int segments = 12)
    {
        if (width < height)
            throw new ArgumentException(" Width must be larger or equal to height.");

        if (!polyline.IsValid)
            return new();

        segments *= 2;
        polyline = new(polyline);
        bool isClosed = polyline.IsClosed;

        if (isClosed)
            polyline.RemoveAt(polyline.Count - 1);

        int last = polyline.Count - 1;

        width *= 0.5;
        height *= 0.5;
        List<Point3d> profile = new(segments / 2);
        double step = PI / ((segments / 2) - 1);

        for (int i = 0; i < segments / 2; i++)
        {
            double angle = i * step - HalfPI;
            Point3d vertex = new(Cos(angle) * height, Sin(angle) * height, 0);
            profile.Add(vertex);
        }

        List<(Plane plane, double scale)> planes = new(polyline.Count);

        for (int i = 0; i < polyline.Count; i++)
        {
            Point3d p = polyline[i];
            Vector3d va = p - (i == 0 ? polyline[last] : polyline[i - 1]);
            Vector3d vb = (i == last ? polyline[0] : polyline[i + 1]) - p;

            va.Unitize();
            vb.Unitize();

            Vector3d vz = va + vb;

            if (!isClosed)
            {
                if (i == 0)
                    vz = vb;

                if (i == polyline.Count - 1)
                    vz = va;
            }

            Vector3d vy = Vector3d.ZAxis;
            var vx = Vector3d.CrossProduct(-vz, vy);
            Point3d origin = p - (Vector3d.ZAxis * height);
            Plane plane = new(origin, vx, vy);
            double scale = width;

            if (isClosed || (i > 0 && i < last))
            {
                var angle = Vector3d.VectorAngle(-va, vb) * 0.5;

                if (angle < PI * 0.25)
                    angle = PI * 0.25;

                scale = width / Sin(angle);
            }

            planes.Add((plane, scale));
        }

        int vertexCount = planes.Count * segments;

        List<Point3d> points = new(vertexCount);
        List<Vector3f> normals = new(vertexCount);

        foreach (var (plane, scale) in planes)
        {
            Polyline pl = new(segments);

            foreach (Point3d point in profile)
            {
                var vertex = plane.PointAt(point.X + scale - height, point.Y);
                pl.Add(vertex);
            }

            foreach (Point3d point in profile)
            {
                var vertex = plane.PointAt(-point.X - scale + height, -point.Y);
                pl.Add(vertex);
            }

            points.AddRange(pl);
            pl.Add(pl[0]);
            var normal = pl.GetNormals().Select(n => (Vector3f)n);

            normals.AddRange(normal);
        }

        List<MeshFace> faces = [];
        int count = isClosed ? vertexCount : vertexCount - segments;

        for (int i = 0; i < count; i++)
        {
            int k = i + 1;
            int j = (k % segments == 0) ? k - segments : k;

            int sj = j + segments;
            int si = i + segments;
            if (i >= points.Count - segments)
            {
                sj -= points.Count;
                si -= points.Count;
            }

            faces.Add(new(i, j, sj, si));
        }

        Mesh mesh = new();
        mesh.Vertices.AddVertices(points);
        mesh.Normals.AddRange([.. normals]);
        mesh.Faces.AddFaces(faces);

        return mesh;
    }

    public static Mesh MeshExtrusion3d(List<Plane> inPlanes, double width, double height, int segments = 12)
    {
        if (inPlanes.Count < 2)
            return new();

        inPlanes = [.. inPlanes];

        bool isClosed = inPlanes[0].Origin.DistanceToSquared(inPlanes[inPlanes.Count - 1].Origin) < UnitTol * UnitTol;

        if (isClosed && inPlanes.Count == 2)
            return new();

        if (isClosed)
            inPlanes.RemoveAt(inPlanes.Count - 1);

        int last = inPlanes.Count - 1;

        segments *= 2;
        width *= 0.5;
        height *= 0.5;
        List<Point3d> profile = new(segments / 2);
        double step = PI / ((segments / 2) - 1);

        for (int i = 0; i < segments / 2; i++)
        {
            double angle = i * step - HalfPI;
            Point3d vertex = new(Cos(angle) * height, Sin(angle) * height, 0);
            profile.Add(vertex);
        }

        List<(Plane plane, double scale)> planes = new(inPlanes.Count);

        for (int i = 0; i < inPlanes.Count; i++)
        {
            Point3d p = inPlanes[i].Origin;
            Vector3d va = p - (i == 0 ? inPlanes[last].Origin : inPlanes[i - 1].Origin);
            Vector3d vb = (i == last ? inPlanes[0].Origin : inPlanes[i + 1].Origin) - p;

            va.Unitize();
            vb.Unitize();

            Vector3d vz = va + vb;

            if (!isClosed)
            {
                if (i == 0)
                    vz = vb;

                if (i == inPlanes.Count - 1)
                    vz = va;
            }

            Vector3d vy = inPlanes[i].Normal;
            var vx = Vector3d.CrossProduct(-vz, vy);
            Point3d origin = p - (Vector3d.ZAxis * height);
            Plane plane = new(origin, vx, vy);
            double scale = width;

            if (isClosed || (i > 0 && i < last))
            {
                var angle = Vector3d.VectorAngle(-va, vb) * 0.5;

                if (angle < PI * 0.25)
                    angle = PI * 0.25;

                scale = width / Sin(angle);
            }

            planes.Add((plane, scale));
        }

        int vertexCount = planes.Count * segments;

        List<Point3d> points = new(vertexCount);
        List<Vector3f> normals = new(vertexCount);

        foreach (var (plane, scale) in planes)
        {
            Polyline pl = new(segments);

            foreach (Point3d point in profile)
            {
                var vertex = plane.PointAt(point.X + scale - height, point.Y);
                pl.Add(vertex);
            }

            foreach (Point3d point in profile)
            {
                var vertex = plane.PointAt(-point.X - scale + height, -point.Y);
                pl.Add(vertex);
            }

            points.AddRange(pl);
            pl.Add(pl[0]);
            var normal = pl.GetNormals().Select(n => (Vector3f)n);

            normals.AddRange(normal);
        }

        List<MeshFace> faces = [];
        int count = isClosed ? vertexCount : vertexCount - segments;

        for (int i = 0; i < count; i++)
        {
            int k = i + 1;
            int j = (k % segments == 0) ? k - segments : k;

            int sj = j + segments;
            int si = i + segments;
            if (i >= points.Count - segments)
            {
                sj -= points.Count;
                si -= points.Count;
            }

            faces.Add(new(i, j, sj, si));
        }

        Mesh mesh = new();
        mesh.Vertices.AddVertices(points);
        mesh.Normals.AddRange([.. normals]);
        mesh.Faces.AddFaces(faces);

        return mesh;
    }

    public static Polyline Fillet(Polyline polyline, double radius = 2)
    {
        polyline = new(polyline);

        if (polyline.Count < 3)
            return polyline;

        bool isClosed = polyline.IsClosed;

        if (isClosed)
        {
            polyline.RemoveAt(polyline.Count - 1);
        }

        Polyline fillet = new(isClosed ? polyline.Count * 3 : (polyline.Count - 2) * 3 + 2);

        for (int i = 0; i < polyline.Count; i++)
        {
            var p = polyline[i];
            if ((i == 0 || i == polyline.Count - 1) && !isClosed)
            {
                fillet.Add(p);
                continue;
            }

            var r = radius;
            var prev = i == 0 ? polyline[polyline.Count - 1] : polyline[i - 1];
            var next = i == polyline.Count - 1 ? polyline[0] : polyline[i + 1];

            var va = prev - p;
            var vb = next - p;
            var aLength = va.Length;
            var bLength = vb.Length;

            var minLength = Min(aLength, bLength);

            if (minLength < Tol)
            {
                fillet.Add(p);
                continue;
            }

            va.Unitize();
            vb.Unitize();

            var vmid = va + vb;

            if (vmid.IsTiny(Tol))
            {
                fillet.Add(p);
                continue;
            }

            vmid.Unitize();

            var angle = Vector3d.VectorAngle(va, vb) * 0.5;
            var tan = Tan(angle);
            var length = r / tan;

            bool isShort = length > minLength * 0.5;
            bool aIsShort = length > aLength * 0.5;

            if (isShort)
            {
                length = minLength * 0.5;
                r = tan * length;
            }

            var mid = Sqrt((r * r) + (length * length)) - r;

            if (!aIsShort || (i == 1 && !isClosed))
                fillet.Add(p + (va * length));
            fillet.Add(p + (vmid * mid));
            fillet.Add(p + (vb * length));
        }

        if (isClosed)
        {
            fillet.Add(fillet[0]);
        }

        return fillet;
    }
}
