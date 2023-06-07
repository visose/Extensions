using Rhino.Geometry;
using Extensions.Spatial;
using ClipperLib;
using static Extensions.Util;
using System.Collections.Concurrent;

namespace Extensions.Simulations.DifferentialGrowth;

public class DifferentialGrowth
{
    internal List<Particle> Particles = new();
    internal List<Spring> Springs = new();
    internal BucketSearchDense3d<Particle> Search;
    internal double Radius;

    public double Growth;
    public List<IntPoint> Region;
    public Mesh Mesh;
    public Polyline Polyline;
    public List<List<Polyline>> AllPolylines = new();
    public Polyline Boundary;
    readonly int _convergence;

    public DifferentialGrowth(IEnumerable<Polyline> polylines, double radius, int convergence, int maxIterations, Polyline region = null, Mesh mesh = null)
    {
        Region = region?.Select(p => new IntPoint(p.X / Tol, p.Y / Tol)).ToList();
        Polyline = region;
        Mesh = mesh;
        Radius = radius;
        Growth = radius * 0.5;
        _convergence = convergence;
        Boundary = mesh?.GetNakedEdges().First();

        var box = Polyline != null ? Polyline.BoundingBox : mesh.GetBoundingBox(true);

        Search = new BucketSearchDense3d<Particle>(box, radius);

        foreach (var polyline in polylines)
        {
            if (polyline == null) continue;
            var startPl = new Particle(polyline[0], this);

            int j = polyline.IsClosed ? -2 : -1;

            for (int i = 1; i < polyline.Count + j; i++)
            {
                var start = Particles[Particles.Count - 1];
                var end = new Particle(polyline[i], this);
                new Spring(start, end, Springs.Count, this);
            }

            if (polyline.IsClosed)
            {
                new Spring(Particles[Particles.Count - 1], startPl, Springs.Count, this);
            }
        }

        AllPolylines.Add(GetPolylines());
        double lastLength = double.MaxValue;
        int count = 0;

        for (count = 0; count < maxIterations; ++count)
        {
            Update();
            AllPolylines.Add(GetPolylines());
            double length = GetLengthSquared();
            if (Math.Abs(lastLength - length) < 1) break;
            lastLength = length;
        }
    }

    void Grow()
    {
        Parallel.ForEach(Partitioner.Create(0, Springs.Count), range =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                var spring = Springs[i];
                spring.RestLength = spring.Length + Growth;
            }
        });
    }

    void Split()
    {
        for (int i = Springs.Count - 1; i >= 0; i--)
        {
            var spring = Springs[i];
            if (spring.Length > Radius * 0.5)
                spring.Split(i);
        }
    }

    void Update()
    {
        Split();
        Grow();

        int iterations = _convergence;
        double totVel;

        do
        {
            Search.Populate(Particles);

            if (Mesh != null)
            {
                var particlesCount = Particles.Count;
                var points = new List<Point3d>(particlesCount);
                var pullPoints = new Point3d[particlesCount];

                for (int i = 0; i < particlesCount; i++)
                    points.Add(Particles[i].Position);

                Parallel.ForEach(Partitioner.Create(0, particlesCount), range =>
                {
                    var subPoints = points.GetRange(range.Item1, range.Item2 - range.Item1);
                    var subPulled = Mesh.PullPointsToMesh(subPoints);

                    int count = 0;
                    for (int i = range.Item1; i < range.Item2; i++)
                        pullPoints[i] = subPulled[count++];
                });

                // var pullPoints = Mesh.PullPointsToMesh(points);

                Parallel.ForEach(Partitioner.Create(0, particlesCount), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        Particles[i].Forces(pullPoints[i]);
                });
            }
            else
            {
                Parallel.ForEach(Partitioner.Create(0, Particles.Count), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                        Particles[i].Forces();
                });
            }

            Parallel.ForEach(Partitioner.Create(0, Springs.Count), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    Springs[i].Forces(10);
            });

            Parallel.ForEach(Partitioner.Create(0, Particles.Count), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    Particles[i].Move();
            });

            Parallel.ForEach(Partitioner.Create(0, Springs.Count), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    Springs[i].Update();
            });

            totVel = 0;
            foreach (var particle in Particles)
                totVel += particle.Velocity.SquareLength;
        } while (totVel > 0.001 && iterations-- > 0);
    }

    public List<Polyline> GetPolylines()
    {
        var polylines = new List<Polyline>();
        Polyline pl = new()
        {
            Springs[0].Start.Position,
            Springs[0].End.Position
        };

        for (int i = 1; i < Springs.Count; i++)
        {
            if (Springs[i - 1].End != Springs[i].Start)
            {
                polylines.Add(pl);
                pl = new Polyline
                    {
                        Springs[i].Start.Position,
                        Springs[i].End.Position
                    };
            }
            else
            {
                pl.Add(Springs[i].End.Position);
            }
        }
        polylines.Add(pl);
        return polylines;
    }

    public double GetLengthSquared()
    {
        return Springs.Sum(s => s.Vector.SquareLength);
    }
}
