using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Rhino.Geometry;
using Rhino.UI;
using Extensions.Model.Geometry;
using Extensions.Model.Document;
using MoreLinq;
using static System.Math;

namespace Extensions.Model.Toolpaths
{
    class Column
    {
        public Polyline[][] Layers { get; }
        public Mesh[][] Pipes { get; }
        public Polyline[] Contours { get; }
        public Mesh Skin { get; }

        public Column(Mesh mesh, double diameter, double height, Interval region)
        {
            double width = Util.GetWidth(diameter, height);
            double offset = width * 0.5;
            const double fillSize = 400;
            const double shortenDist = 10;

            var watch = new Stopwatch();
            string _text = null;

            Progress("Contouring");
            var contours = Contouring(mesh);

            Progress("Cleaning");
            var cleanContours = Cleaning(contours);

            Progress("FixCantilevers");
            cleanContours = FixCantilevers(cleanContours);

            Progress("Toolpathing");
            var toolpath = Toolpathing(cleanContours);

            Progress("Piping");
            var pipes = Piping(toolpath);

            Progress("Skin");
            var skin = new Mesh();
            for (int i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i].Last();
                skin.Append(pipe);
            }

            Progress("end");

            Contours = contours;
            Layers = toolpath;//new Polyline[0][];
            Pipes = pipes; //new Mesh[0][];
            Skin = skin;

            Polyline[] Contouring(Mesh m)
            {
                Polyline[] outPolylines;
                var contour = Cache.Read<Curve>("contours")?.ToPolyline();

                if (contour is null)
                {
                    outPolylines = Slicer.Create(m, height, region)
                                  .Select(c => c.MaxBy(p => p.Length).First())
                                  .ToArray();

                    Cache.Write<Curve>("contours", new PolylineCurve(outPolylines[0]));
                }
                else
                {
                    outPolylines = Enumerable.Repeat(contour, 1).ToArray();
                }

                return outPolylines;
            }

            Polyline[] Cleaning(Polyline[] inPolylines)
            {
                var outPolylines = new Polyline[inPolylines.Length];

                Parallel.ForEach(Partitioner.Create(0, inPolylines.Length), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var polyline = inPolylines[i];
                        polyline = Shorten(polyline, shortenDist);
                        //polyline = BallPivot.Create(polyline, width * 0.2); // simplfy
                        outPolylines[i] = polyline;
                    }
                });

                Polyline Shorten(Polyline contour, double resolution)
                {
                    var maxDist = resolution * 1;
                    var maxDistSq = maxDist * maxDist;
                    var pivot = BallPivot.Create(contour, resolution);

                    var outCurve = new Polyline(contour.Where(p => pivot.ClosestPoint(p).DistanceToSquared(p) < maxDistSq));
                    if (!outCurve.IsClosed)
                        outCurve.Add(outCurve[0]);

                    return outCurve;
                }

                return outPolylines;
            }

            Mesh[][] Piping(Polyline[][] inPolylines)
            {
                var outMeshes = new Mesh[inPolylines.Length][];
                if (inPolylines.Length == 0) return outMeshes;

                Parallel.ForEach(Partitioner.Create(0, inPolylines.Length), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        //var pls = inPolylines[i].Select(p => p.ToPolyline(0.01, PI * 0.01, 1, double.MaxValue).ToPolyline());
                        outMeshes[i] = inPolylines[i].Select(p => MeshPipe.MeshFlatPolyline(p, width, height, width * 0.5, 5)).ToArray();
                    }
                });

                return outMeshes;
            }

            Polyline[][] Toolpathing(Polyline[] inPolylines)
            {
                var outPolylines = new Polyline[inPolylines.Length][];

                Parallel.ForEach(Partitioner.Create(0, inPolylines.Length), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        // var pl = inPolylines[i];
                        var outerSkin = inPolylines[i];
                        //var outerSkin = Geometry.Region.Offset(pl, -offset);
                        var innerSkin = Geometry.Region.Offset(outerSkin, -offset * 2);
                        var outPolyline = new Polyline[] { outerSkin, innerSkin };

                        outPolylines[i] = outPolyline.Select(p => Clean(p, offset)).ToArray();
                    }
                });

                //   Progress("Filling");
                var layers = PolygonFill.Square(outPolylines.Select(l => l[1]).ToArray(), fillSize, offset);

                for (int i = 0; i < inPolylines.Length; i++)
                {
                    outPolylines[i] = MoreEnumerable.Append(layers[i], outPolylines[i][0]).ToArray();
                }

                return outPolylines;

                Polyline Clean(Polyline contour, double resolution)
                {
                    var clean = new Polyline(contour);
                    var removed = clean.ReduceSegments(resolution * 0.02);
                    removed = clean.CollapseShortSegments(2);
                    //var nurbs = Curve.CreateInterpolatedCurve(clean, 3);
                    //Document.Debug.Bake(nurbs, Color.Red);
                    //var curve = nurbs.ToArcsAndLines(resolution * 0.1, PI * 0.1, width * 0.5, double.MaxValue);
                    // Document.Debug.Bake(curve, Color.Blue);
                    //var pl = curve.ToPolyline(0.01, PI * 0.01, 0.1, double.MaxValue);
                    //Document.Debug.Bake(pl, Color.Green);
                    return clean;
                }
            }

            Polyline[] FixCantilevers(Polyline[] polylines)
            {
                Parallel.ForEach(Partitioner.Create(0, polylines.Length), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var contour = Geometry.Region.Offset(polylines[i], -offset);
                        var removed = contour.ReduceSegments(width * 0.02);
                        removed = contour.CollapseShortSegments(2);
                        polylines[i] = contour;
                    }
                });

                double maxSeparation = width * 0.75;
                double maxSeparationSq = maxSeparation * maxSeparation;

                for (int i = 1; i < polylines.Length; i++)
                {
                    var contour = polylines[i];
                    var prev = polylines[i - 1];

                    for (int j = 0; j < contour.Count; j++)
                    {
                        var p = contour[j];
                        var closest = prev.ClosestPointFast(p);
                        var v = closest - p;
                        v.Z = 0;
                        var lengthSq = v.SquareLength;
                        if (lengthSq > maxSeparationSq)
                        {
                            var length = Sqrt(lengthSq);
                            var move = v * ((length - maxSeparation) / length);
                            p += move;
                            contour[j] = p;
                        }
                    }
                }

                return polylines;
            }

            void Progress(string text)
            {
                if (text != "Contouring") Rhino.RhinoApp.WriteLine($"{ _text}: {watch.ElapsedMilliseconds}");
                StatusBar.HideProgressMeter();
                if (text == "end") return;
                _text = text;
                StatusBar.ShowProgressMeter(0, 4, $"{text}...", true, true);
                if (text != "Contouring") StatusBar.UpdateProgressMeter(1, true);
                watch.Restart();
            }
        }
    }
}
