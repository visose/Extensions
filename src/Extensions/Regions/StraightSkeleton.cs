using SkeletonNet;
using Rhino.Geometry;
using SkeletonVector2d = SkeletonNet.Primitives.Vector2d;

namespace Extensions.StraightSkeleton;

public static class StraightSkeleton
{
    public static IEnumerable<Polyline> GetStraightSkeleton(Polyline polyline)
    {
        if (!polyline.IsClosed)
            throw new ArgumentException("Polyline must be closed.", nameof(polyline));

        var polygon = polyline.Select(p => p.ToVector2d()).ToList();
        polygon.RemoveAt(polygon.Count - 1);
        var skeleton = SkeletonBuilder.Build(polygon);

        return skeleton.Edges.Select(e =>
            {
                Polyline region = new(e.Polygon.Select(v => v.ToPoint3d()));
                region.Add(region[0]);
                return region;
            }
        );
    }

    static bool Exists(Point3d point, PointCloud pc)
    {
        int index = pc.ClosestPoint(point);

        if (index == -1)
            return false;

        Point3d closest = pc[index].Location;
        return (closest - point).SquareLength < 0.0001;
    }

    public static Polyline GetAxis(IEnumerable<Polyline> regions, Polyline boundary)
    {
        var lines = regions.SelectMany(e => e.GetSegments());
        PointCloud pc = new(boundary.GetSegments().Select(l => l.PointAt(0.5)));

        var edges = lines.Select(l => (Line: l, Center: l.PointAt(0.5)));
        var interiorEdges = edges.Where(e => !Exists(e.Center, pc));
        List<Line> interiorLines = [];

        foreach (var edge in interiorEdges)
        {
            if (Exists(edge.Center, pc))
                continue;

            pc.Add(edge.Center);
            interiorLines.Add(edge.Line);
        }

        return GetAxis(interiorLines);
    }

    public static Polyline GetAxis(IEnumerable<Line> lines)
    {
        var allPoints = lines.SelectMany(static l => new Point3d[] { l.From, l.To });
        PointCloud pc = new();

        foreach (var point in allPoints)
        {
            if (Exists(point, pc))
                continue;
            pc.Add(point);
        }

        var nodes = Enumerable.Range(0, pc.Count).Select(_ => new List<Edge>()).ToList();

        var edges = lines.Select(l =>
        {
            Edge edge = new() { Start = pc.ClosestPoint(l.From), End = pc.ClosestPoint(l.To), Line = l, Enabled = true };
            nodes[edge.Start].Add(edge);
            nodes[edge.End].Add(edge);
            return edge;
        }).ToList();

        while (true)
        {
            List<Edge> toDisable = [];

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                int valence = node.Count(e => e.Enabled);

                if (valence == 1)
                {
                    var edge = node.First(e => e.Enabled);
                    var j = edge.Start == i ? edge.End : edge.Start;

                    int otherValence = nodes[j].Count(e => e.Enabled);
                    if (otherValence >= 3)
                        toDisable.Add(edge);
                }
            }

            if (toDisable.Count == 0)
                break;

            foreach (var edge in toDisable)
                edge.Enabled = false;
        }

        var axes = edges
            .Where(e => e.Enabled)
            .Select(e => e.Line.ToNurbsCurve());

        var curve = Curve.JoinCurves(axes);
        Polyline pl = [];

        if (curve.Length > 0)
        {
            curve[0].TryGetPolyline(out pl);
        }
        else
        {
            Line maxLine = Line.Unset;
            double maxLength = 0;

            foreach (var line in lines)
            {
                double length = line.Direction.SquareLength;
                if (length > maxLength)
                {
                    maxLength = length;
                    maxLine = line;
                }
            }

            pl = new([maxLine.From, maxLine.To]);
        }

        return pl;
    }

    class Edge
    {
        public int Start;
        public int End;
        public Line Line;
        public bool Enabled;
    }

    extension(Point3d point)
    {
        private SkeletonVector2d ToVector2d()
        {
            point /= 0.1;
            return new(point.X, point.Y);
        }
    }

    extension(SkeletonVector2d vector)
    {
        private Point3d ToPoint3d()
        {
            Point3d point = new(vector.X, vector.Y, 0);
            point *= 0.1;
            return point;
        }
    }
}
