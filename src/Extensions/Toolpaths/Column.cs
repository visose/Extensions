using System.Collections.Concurrent;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using Extensions.Geometry;
using Extensions.Document;
using MoreLinq;
using static System.Math;

namespace Extensions.Toolpaths;

public class Column
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

        string? _text = null;

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
        Mesh skin = new();

        for (int i = 0; i < pipes.Length; i++)
        {
            var pipe = pipes[i].Last();
            skin.Append(pipe);
        }

        Progress("end");

        Contours = contours;
        Layers = toolpath;
        Pipes = pipes;
        Skin = skin;

        Polyline[] Contouring(Mesh m)
        {
            Polyline[] outPolylines;
            var contour = Cache.Read<Curve>("contours")?.ToPolyline();

            if (contour is null)
            {
                outPolylines = Slicer.Create(m, height, region)
                              .Select(c => c.Maxima(p => p.Length).First())
                              .ToArray();

                Cache.Write("contours", new PolylineCurve(outPolylines[0]));
            }
            else
            {
                outPolylines = [contour];
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
                    outPolylines[i] = polyline;
                }
            });

            Polyline Shorten(Polyline contour, double resolution)
            {
                var maxDist = resolution * 1;
                var maxDistSq = maxDist * maxDist;
                var pivot = BallPivot.Create(contour, resolution);

                Polyline outCurve = new(contour.Where(p => pivot.ClosestPoint(p).DistanceToSquared(p) < maxDistSq));

                if (!outCurve.IsClosed)
                    outCurve.Add(outCurve[0]);

                return outCurve;
            }

            return outPolylines;
        }

        Mesh[][] Piping(Polyline[][] inPolylines)
        {
            var outMeshes = new Mesh[inPolylines.Length][];

            if (inPolylines.Length == 0)
                return outMeshes;

            Parallel.ForEach(Partitioner.Create(0, inPolylines.Length), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
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
                    var outerSkin = inPolylines[i];
                    var innerSkin = Geometry.Region.Offset(outerSkin, -offset * 2);
                    Polyline[] outPolyline = [outerSkin, innerSkin];

                    outPolylines[i] = outPolyline.Select(p => Clean(p, offset)).ToArray();
                }
            });

            var layers = PolygonFill.Square(outPolylines.Select(l => l[1]).ToArray(), fillSize, offset);

            for (int i = 0; i < inPolylines.Length; i++)
            {
                outPolylines[i] = MoreEnumerable.Append(layers[i], outPolylines[i][0]).ToArray();
            }

            return outPolylines;

            Polyline Clean(Polyline contour, double resolution)
            {
                Polyline clean = new(contour);
                clean.ReduceSegments(resolution * 0.02);
                clean.CollapseShortSegments(2);
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
                    contour.ReduceSegments(width * 0.02);
                    contour.CollapseShortSegments(2);
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
            if (text != "Contouring")
                RhinoApp.WriteLine($"{_text}");
            StatusBar.HideProgressMeter();

            if (text == "end")
                return;

            _text = text;
            StatusBar.ShowProgressMeter(0, 4, $"{text}...", true, true);
            if (text != "Contouring")
                StatusBar.UpdateProgressMeter(1, true);
        }
    }
}
