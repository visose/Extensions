using Rhino.Geometry;
using Clipper2Lib;
using Extensions.Geometry;
using Extensions.Spatial;
using System.Collections.Concurrent;

namespace Extensions.Simulations.DifferentialGrowth;

public sealed class DifferentialGrowth
{
    readonly List<List<Polyline>> _allPolylines = [];
    readonly int _convergence;

    internal List<Particle> Particles { get; } = [];
    internal List<Spring> Springs { get; } = [];
    internal BucketSearchDense3d<Particle> Search { get; }
    internal double Radius { get; }
    internal double Growth { get; }
    internal Path64? Region { get; }
    internal Mesh? Mesh { get; }
    internal Polyline? Polyline { get; }

    public IReadOnlyList<IReadOnlyList<Polyline>> AllPolylines => _allPolylines;

    public DifferentialGrowth(IReadOnlyList<Polyline> polylines, double radius, int convergence, int maxIterations, Polyline? region = null, Mesh? mesh = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(polylines.Count, nameof(polylines));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(radius));
        ArgumentOutOfRangeException.ThrowIfNegative(convergence, nameof(convergence));
        ArgumentOutOfRangeException.ThrowIfNegative(maxIterations, nameof(maxIterations));

        var box = (region, mesh) switch
        {
            ({ } polyline, _) => polyline.BoundingBox,
            (_, { } targetMesh) => targetMesh.GetBoundingBox(true),
            _ => throw new ArgumentException("Either a planar region or a mesh is required.", nameof(region))
        };

        Region = region?.ToRegion();
        Polyline = region;
        Mesh = mesh;
        Radius = radius;
        Growth = radius * 0.5;
        _convergence = convergence;

        Search = new(box, radius);

        foreach (var polyline in polylines)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(polyline.Count, 2, nameof(polylines));

            Particle startPl = new(polyline[0], this);

            int endIndex = polyline.IsClosed ? polyline.Count - 1 : polyline.Count;

            for (int i = 1; i < endIndex; i++)
            {
                var start = Particles[Particles.Count - 1];
                Particle end = new(polyline[i], this);
                new Spring(start, end, Springs.Count, this);
            }

            if (polyline.IsClosed)
            {
                new Spring(Particles[Particles.Count - 1], startPl, Springs.Count, this);
            }
        }

        ArgumentOutOfRangeException.ThrowIfZero(Springs.Count, nameof(polylines));

        _allPolylines.Add(GetPolylines());
        double lastLength = double.MaxValue;
        int count = 0;

        for (count = 0; count < maxIterations; ++count)
        {
            Update();
            _allPolylines.Add(GetPolylines());
            double length = GetLengthSquared();

            if (Math.Abs(lastLength - length) < 1)
                break;

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
                List<Point3d> points = new(particlesCount);
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

    List<Polyline> GetPolylines()
    {
        List<Polyline> polylines = [];
        Polyline pl =
        [
            Springs[0].Start.Position,
            Springs[0].End.Position
        ];

        for (int i = 1; i < Springs.Count; i++)
        {
            if (Springs[i - 1].End != Springs[i].Start)
            {
                polylines.Add(pl);
                pl =
                    [
                        Springs[i].Start.Position,
                        Springs[i].End.Position
                    ];
            }
            else
            {
                pl.Add(Springs[i].End.Position);
            }
        }
        polylines.Add(pl);
        return polylines;
    }

    double GetLengthSquared()
    {
        return Springs.Sum(s => s.Vector.SquareLength);
    }
}
