using Rhino.Geometry;
using Extensions.Spatial;
using MoreLinq;

namespace Extensions.Geometry;

internal class BallPivot
{
    public static Polyline Create(IEnumerable<Polyline> contour, double radius)
    {
        return new BallPivot(contour, radius).Polyline;
    }

    public static Polyline Create(Polyline contour, double radius)
    {
        return new BallPivot(Enumerable.Repeat(contour, 1), radius).Polyline;
    }

    readonly BucketSearchSparse2d<Point> _search;
    readonly List<Point> _points;
    BoundingBox _box;
    int _start;
    Polyline Polyline { get; }

    BallPivot(IEnumerable<Polyline> contour, double radius)
    {
        contour = contour
            .MaxBy(p => p.Length)
            .Select(FixDirection);

        var polylines = DivideCurves(contour, radius).ToArray();
        var points = polylines.SelectMany(p => p);

        if (!points.Any())
        {
            Polyline = new Polyline();
            return;
        }

        var normals = polylines.SelectMany(p => p.GetNormals());

        int i = 0;
        _points = points.Zip(normals, (p, n) => new Point() { Position = p, Normal = n, Index = i++ }).ToList();
        _search = new BucketSearchSparse2d<Point>(radius);
        _search.Populate(_points);
        _box = new BoundingBox(points);

        Polyline = Pivot();
    }

    Polyline FixDirection(Polyline polyline)
    {
        int count = polyline.Count;
        var a = polyline[0];
        var b = polyline[(int)(count * (1 / 3.0))];
        var c = polyline[(int)(count * (2 / 3.0))];
        var n = Vector3d.CrossProduct(b - a, c - a);
        if (n.Z > 0)
        {
            var pl = new Polyline(polyline);
            pl.Reverse();
            return pl;
        }

        return polyline;
        //Document.Debug.Bake(polyline,System.Drawing.Color.Green);
    }

    IEnumerable<Polyline> DivideCurves(IEnumerable<Polyline> contour, double radius)
    {
        foreach (var pl in contour)
        {
            var curve = new PolylineCurve(pl);
            curve.DivideByLength(radius * 0.25, true, false, out Point3d[] points);

            if (points == null || points.Length < 3)
                yield return new Polyline();
            else
                yield return new Polyline(points);
        }
    }

    Polyline Pivot()
    {
        Point first = null;
        Point second = _points.MinBy(p => p.Position.DistanceToSquared(_box.Max)).First();
        _start = second.Index;

        Polyline pl = new Polyline() { second.Position };

        while (true)
        {
            var next = FindNext(first, second);
            if (next == null)
                break;
            first = second;
            second = next;
            pl.Add(next.Position);
        }

        pl.Add(pl[0]);
        return pl;
    }

    Point FindNext(Point a, Point b)
    {
        int indexb = Index(b);
        var closests = _search.GetClosests(b);

        //if (a != null)
        {
            closests = closests.Where(p => Index(p) > indexb && ((Index(p) - indexb) < _points.Count / 2));
        }

        if (!closests.Any())
            return null;

        Vector3d vector = a == null ? b.Normal : a.Position - b.Position;

        var next = closests
            .MinBy(p => Vector3d.VectorAngle(vector, p.Position - b.Position, -Vector3d.ZAxis))
            .First();

        return next;

        int Index(Point p) => p.Index < _start ? _points.Count - 1 + p.Index : p.Index;
    }
}

class Point : IPositionable
{
    Point3d _p;
    public ref Point3d Position => ref _p;
    public Vector3d Normal { get; set; }
    public int Index { get; set; }
}
