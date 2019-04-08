using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using static System.Math;
using static Extensions.Model.Util;

namespace Extensions.Model.Geometry
{
    static class MeshPipe
    {
        public static Mesh MeshFlatPolyline(Polyline polyline, double width, double height, double fillet = 0, int segments = 24)
        {
            if (fillet > 0)
            {
                polyline = Fillet(polyline, fillet);
                //polyline.ReduceSegments(0.01);
                //polyline.CollapseShortSegments(0.1);
                // polyline.MergeColinearSegments(PI * 0.01, true);
            }
            //var planes = polyline.Select(p => new Plane(p, Vector3d.ZAxis)).ToList();
            return MeshExtrusion(polyline, width, height, segments);
        }

        public static Mesh MeshPlanes(List<Plane> inPlanes, double width, double height, int segments = 24)
        {
            inPlanes = inPlanes.ToList();
            if (inPlanes.Count < 2) return new Mesh();

            bool isClosed = inPlanes[0].Origin.DistanceToSquared(inPlanes[inPlanes.Count - 1].Origin) < UnitTol * UnitTol;
            if (isClosed && inPlanes.Count == 2) return new Mesh();

            if (isClosed) inPlanes.RemoveAt(inPlanes.Count - 1);
            int last = inPlanes.Count - 1;

            width *= 0.5;
            height *= 0.5;
            var profile = new List<Point3d>();
            double step = PI2 / segments;

            for (int i = 0; i < segments; i++)
            {
                double angle = i * step;
                var vertex = new Point3d(Cos(angle) * width, Sin(angle) * height, 0);
                profile.Add(vertex);
            }

            var planes = new List<(Plane, double)>(inPlanes.Count);

            for (int i = 0; i < inPlanes.Count; i++)
            {
                Point3d p = inPlanes[i].Origin;
                Vector3d vz = Vector3d.Zero;
                Vector3d va = Vector3d.Zero;
                Vector3d vb = Vector3d.Zero;

                int prev = i - 1;
                if (prev < 0) prev += last + 1;
                va = p - inPlanes[prev].Origin;

                int next = i + 1;
                if (next > last) next -= (last + 1);
                vb = inPlanes[next].Origin - p;


                va.Unitize();
                vb.Unitize();

                vz = va + vb;

                if (!isClosed)
                {
                    if (i == 0)
                        vz = inPlanes[1].Origin - inPlanes[0].Origin;

                    if (i == last)
                        vz = inPlanes[last].Origin - inPlanes[last - 1].Origin;
                }

                if (vz.IsTiny())
                {
                    vz = va;
                    //throw new Exception();
                }

                vz.Unitize();

                Vector3d vy = inPlanes[i].Normal;
                Vector3d vx = Vector3d.CrossProduct(-vz, vy);
                Point3d origin = inPlanes[i].Origin - (inPlanes[i].Normal * height);
                var plane = new Plane(origin, vx, vy);
                double scale = 1;

                if (isClosed || (i > 0 && i < last))
                {
                    var angle = Vector3d.VectorAngle(-va, vb) * 0.5;
                    if (angle < PI * 0.1) angle = PI * 0.1;
                    scale = 1 / Sin(angle);
                }

                bool isPlaneValid = plane.IsValid;

                if (!isPlaneValid)
                {
                    throw new Exception();
                }

                planes.Add((plane, scale));
            }

            int vertexCount = planes.Count * segments;

            var points = new List<Point3d>(vertexCount);
            var normals = new List<Vector3f>(vertexCount);

            foreach (var (plane, scale) in planes)
            {
                var pl = new List<Point3d>(segments + 1);
                foreach (Point3d point in profile)
                {
                    var scaledPoint = point;
                    scaledPoint.X *= scale;
                    var vertex = plane.PointAt(scaledPoint.X, scaledPoint.Y);
                    // var normal = vertex - plane.Origin;
                    //normal.Unitize();
                    points.Add(vertex);
                    pl.Add(vertex);
                    //normals.Add(new Vector3f((float)normal.X, (float)normal.Y, (float)normal.Z));
                }

                pl.Add(pl[0]);
                var normal = pl.GetNormals().Select(n => (Vector3f)n).ToArray();
                normals.AddRange(normal);
            }

            var faces = new List<MeshFace>();
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

                faces.Add(new MeshFace(i, j, sj, si));
            }

            var mesh = new Mesh();
            mesh.Faces.AddFaces(faces);
            var pointsf = points.Select(p => new Point3f((float)p.X, (float)p.Y, (float)p.Z));
            mesh.Vertices.AddVertices(pointsf);

            for (int i = 0; i < points.Count; i++)
            {
                var vertex = points[i];
                mesh.Vertices.SetVertex(i, vertex);
            }
            mesh.Normals.AddRange(normals.ToArray());
            mesh.RebuildNormals();
            mesh.Compact();

            var isValid = mesh.IsValid;

            //if(!isValid)
            //    throw new Exception();

            return mesh;
        }

        public static Mesh MeshExtrusion(Polyline polyline, double width, double height, int segments = 12)
        {
            if (width < height) throw new ArgumentException(" Width must be larger or equal to height.");
            if (!polyline.IsValid) return new Mesh(); // throw new ArgumentException(" Invalid polyline.");

            segments *= 2;
            polyline = new Polyline(polyline);
            bool isClosed = polyline.IsClosed;
            if (isClosed) polyline.RemoveAt(polyline.Count - 1);
            int last = polyline.Count - 1;

            width *= 0.5;
            height *= 0.5;
            var profile = new List<Point3d>(segments / 2);
            double step = PI / ((segments / 2) - 1);

            for (int i = 0; i < segments / 2; i++)
            {
                double angle = i * step - HalfPI;
                var vertex = new Point3d(Cos(angle) * height, Sin(angle) * height, 0);
                profile.Add(vertex);
            }

            var planes = new List<(Plane, double)>(polyline.Count);

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
                    if (i == 0) vz = vb;
                    if (i == polyline.Count - 1) vz = va;
                }

                Vector3d vy = Vector3d.ZAxis;
                Vector3d vx = Vector3d.CrossProduct(-vz, vy);
                Point3d origin = p - (Vector3d.ZAxis * height);
                var plane = new Plane(origin, vx, vy);
                double scale = width;

                if (isClosed || (i > 0 && i < last))
                {
                    var angle = Vector3d.VectorAngle(-va, vb) * 0.5;
                    if (angle < PI * 0.25) angle = PI * 0.25;
                    scale = width / Sin(angle);
                }

                planes.Add((plane, scale));
            }

            int vertexCount = planes.Count * segments;

            var points = new List<Point3d>(vertexCount);
            var normals = new List<Vector3f>(vertexCount);

            foreach (var (plane, scale) in planes)
            {
                var pl = new Polyline(segments);

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

            var faces = new List<MeshFace>();
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

                faces.Add(new MeshFace(i, j, sj, si));
            }

            var mesh = new Mesh();
            mesh.Vertices.AddVertices(points);
            mesh.Normals.AddRange(normals.ToArray());
            mesh.Faces.AddFaces(faces);

            return mesh;
        }
        public static Mesh MeshExtrusion3d(List<Plane> inPlanes, double width, double height, int segments = 12)
        {
            //if (width < height) throw new ArgumentException(" Width must be larger or equal to height.");
            if (inPlanes.Count < 2) return new Mesh();
            inPlanes = inPlanes.ToList();

            bool isClosed = inPlanes[0].Origin.DistanceToSquared(inPlanes[inPlanes.Count - 1].Origin) < UnitTol * UnitTol;
            if (isClosed && inPlanes.Count == 2) return new Mesh();

            if (isClosed) inPlanes.RemoveAt(inPlanes.Count - 1);
            int last = inPlanes.Count - 1;

            segments *= 2;
            width *= 0.5;
            height *= 0.5;
            var profile = new List<Point3d>(segments / 2);
            double step = PI / ((segments / 2) - 1);

            for (int i = 0; i < segments / 2; i++)
            {
                double angle = i * step - HalfPI;
                var vertex = new Point3d(Cos(angle) * height, Sin(angle) * height, 0);
                profile.Add(vertex);
            }

            var planes = new List<(Plane, double)>(inPlanes.Count);

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
                    if (i == 0) vz = vb;
                    if (i == inPlanes.Count - 1) vz = va;
                }

                Vector3d vy = inPlanes[i].Normal; //Vector3d.ZAxis;
                Vector3d vx = Vector3d.CrossProduct(-vz, vy);
                Point3d origin = p - (Vector3d.ZAxis * height);
                var plane = new Plane(origin, vx, vy);
                double scale = width;

                if (isClosed || (i > 0 && i < last))
                {
                    var angle = Vector3d.VectorAngle(-va, vb) * 0.5;
                    if (angle < PI * 0.25) angle = PI * 0.25;
                    scale = width / Sin(angle);
                }

                planes.Add((plane, scale));
            }

            int vertexCount = planes.Count * segments;

            var points = new List<Point3d>(vertexCount);
            var normals = new List<Vector3f>(vertexCount);

            foreach (var (plane, scale) in planes)
            {
                var pl = new Polyline(segments);

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

            var faces = new List<MeshFace>();
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

                faces.Add(new MeshFace(i, j, sj, si));
            }

            var mesh = new Mesh();
            mesh.Vertices.AddVertices(points);
            mesh.Normals.AddRange(normals.ToArray());
            mesh.Faces.AddFaces(faces);

            return mesh;
        }

        public static Polyline Fillet(Polyline polyline, double radius = 2)
        {
            polyline = new Polyline(polyline);

            if (polyline.Count < 3)
                return polyline;

            bool isClosed = polyline.IsClosed;

            if (isClosed)
            {
                polyline.RemoveAt(polyline.Count - 1);
            }


            var fillet = new Polyline(isClosed ? polyline.Count * 3 : (polyline.Count - 2) * 3 + 2);

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
}
