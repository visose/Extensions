﻿using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using MoreLinq;
using static Extensions.Model.Util;
using ClipperLib;

namespace Extensions.Model.Geometry
{
    static class Region
    {
        public static Polyline Offset(Polyline polyline, double distance)
        {
            if (polyline.Count < 2) return new Polyline();

            var region = polyline.ToRegion();
            var offset = new ClipperOffset();
            offset.AddPath(region, JoinType.jtRound, EndType.etClosedPolygon);
            PolyTree tree = new PolyTree();
           
            offset.Execute(ref tree, distance / Tol);

            var height = polyline[0].Z;
            var first = tree.ToPolylines(height).MaxBy(p => p.Length).FirstOrDefault();
            return first ?? new Polyline(0);
        }

        public static Polyline[] Intersection(IEnumerable<Polyline> a, IEnumerable<Polyline> b)
        {
            Clipper clipper = new Clipper(Clipper.ioStrictlySimple);
            clipper.AddPaths(a.ToRegions(), PolyType.ptClip, true);
            clipper.AddPaths(b.ToRegions(), PolyType.ptSubject, true);

            PolyTree tree = new PolyTree();
            clipper.Execute(ClipType.ctIntersection, tree);

            double height = b.First()[0].Z;
            return tree.ToPolylines(height);
        }

        public static List<IntPoint> ToRegion(this Polyline polyline)
        {
            return polyline.Select(p => new IntPoint(p.X / Tol, p.Y / Tol)).ToList();
        }

        public static List<List<IntPoint>> ToRegions(this IEnumerable<Polyline> polylines)
        {
            return polylines.Select(ToRegion).ToList();
        }

        public static Polyline[] ToPolylines(this PolyTree tree, double height)
        {
            var polylines = new Polyline[tree.ChildCount];

            for (int i = 0; i < tree.ChildCount; i++)
            {
                var node = tree.Childs[i];
                var pl = new Polyline(node.m_polygon.Select(p => new Point3d(p.X * Tol, p.Y * Tol, height)));
                pl.Add(pl[0]);
                polylines[i] = pl;
            }

            return polylines;
        }
    }
}
