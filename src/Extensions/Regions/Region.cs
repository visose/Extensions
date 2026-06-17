using Rhino.Geometry;
using MoreLinq;
using static Extensions.Util;
using Clipper2Lib;

namespace Extensions.Geometry;

static class Region
{
    public static Polyline Offset(Polyline polyline, double distance)
    {
        if (polyline.Count < 2)
            return [];

        Path64 region = polyline.ToRegion();
        ClipperOffset offset = new();
        offset.AddPath(region, JoinType.Round, EndType.Polygon);
        Paths64 paths = [];

        offset.Execute(distance / Tol, paths);

        var height = polyline[0].Z;
        var first = paths.ToPolylines(height).Maxima(static p => p.Length).FirstOrDefault();
        return first ?? [];
    }

    public static Polyline[] Intersection(IEnumerable<Polyline> a, IEnumerable<Polyline> b)
    {
        var subjectPolylines = b as IReadOnlyList<Polyline> ?? b.ToList();
        var subjects = subjectPolylines.ToRegions();
        Paths64 paths = Clipper.Intersect(subjects, a.ToRegions(), FillRule.NonZero);

        double height = subjectPolylines.Count > 0 ? subjectPolylines[0][0].Z : 0;
        return paths.ToPolylines(height);
    }

    extension(Polyline polyline)
    {
        public Path64 ToRegion()
        {
            return new(polyline.Select(static p => new Point64(p.X / Tol, p.Y / Tol)));
        }
    }

    extension(IEnumerable<Polyline> polylines)
    {
        public Paths64 ToRegions()
        {
            return new(polylines.Select(static polyline => polyline.ToRegion()));
        }
    }

    extension(Paths64 paths)
    {
        public Polyline[] ToPolylines(double height)
        {
            var polylines = new Polyline[paths.Count];

            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                Polyline pl = new(path.Select(p => new Point3d(p.X * Tol, p.Y * Tol, height)));
                if (pl.Count > 0)
                    pl.Add(pl[0]);
                polylines[i] = pl;
            }

            return polylines;
        }
    }
}
