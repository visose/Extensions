using System.Collections.Concurrent;
using Rhino.Geometry;
using static System.Math;

namespace Extensions.Geometry;

static class PolygonFill
{
    public static Polyline[][] Square(Polyline[] contours, double size, double offset)
    {
        if (contours.Length == 0)
            return [];

        BoundingBox box = new(contours.SelectMany(p => p));
        box.Inflate(offset);

        int countX = (int)Ceiling(box.Diagonal.X / size);
        int countY = (int)Ceiling(box.Diagonal.Y / size);

        double stepX = box.Diagonal.X / countX;
        double stepY = box.Diagonal.Y / countY;

        List<Polyline> rectangles = new(countX * countY);

        for (double x = box.Min.X; x < box.Max.X; x += stepX)
        {
            for (double y = box.Min.Y; y < box.Max.Y; y += stepY)
            {
                Rectangle3d rect = new(Plane.WorldXY, new Point3d(x + offset, y + offset, 0), new Point3d(x + stepX - offset, y + stepY - offset, 0));
                rectangles.Add(rect.ToPolyline());
            }
        }

        int layerCount = contours.Length;
        var layers = new Polyline[layerCount][];

        Parallel.ForEach(Partitioner.Create(0, layerCount), range =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                var skinOffset = contours[i];
                if (!skinOffset.IsValid)
                {
                    layers[i] = [];
                    continue;
                }

                var squares = rectangles.SelectMany(r => Region.Intersection([r], [skinOffset])).ToArray();
                layers[i] = squares;
            }
        });

        return layers;
    }
}
