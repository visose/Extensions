using System.Collections.Concurrent;
using Rhino.Geometry;
using static System.Math;

namespace Extensions.Geometry;

class Slicer
{
    public static Polyline[][] Create(Mesh mesh, double height, Interval modelRegion)
    {
        var slicer = new Slicer(mesh, height, modelRegion);
        slicer.Slice();
        return slicer.Contours;
    }

    readonly Mesh _mesh;
    readonly double _height;
    Interval _modelRegion;

    Polyline[][] Contours { get; set; }

    private Slicer(Mesh mesh, double height, Interval modelRegion)
    {
        _mesh = mesh;
        _height = height;
        _modelRegion = modelRegion;
    }

    void Slice()
    {
        var box = _mesh.GetBoundingBox(true);
        var layerCount = (int)Ceiling(box.Diagonal.Z / _height) - 1;
        var planes = Enumerable.Range(0, layerCount).Select(i => new Plane(new Point3d(0, 0, (i + 1) * _height), Vector3d.ZAxis)).ToList();
        var subPlanes = planes.GetRange((int)(_modelRegion.T0 * (planes.Count - 1)), (int)Ceiling(_modelRegion.Length * planes.Count));

        var processors = Environment.ProcessorCount;
        var chunk = (int)Ceiling(subPlanes.Count / (double)processors);
        if (chunk == 0) chunk = 1;
        var partitions = Partitioner.Create(0, subPlanes.Count, chunk);

        Contours = new Polyline[subPlanes.Count][];

        Parallel.ForEach(partitions, range =>
        {
            var loopPlanes = subPlanes.GetRange(range.Item1, range.Item2 - range.Item1);

            var layers = Rhino.Geometry.Intersect.Intersection.MeshPlane(_mesh, loopPlanes)
                               .GroupBy(p => (int)Round(p[0].Z / _height))
                               .Select(p => p.ToArray())
                               .ToArray();

            for (int i = 0; i < layers.Length; i++)
            {
                Contours[range.Item1 + i] = layers[i];
            }
        });
    }
}
