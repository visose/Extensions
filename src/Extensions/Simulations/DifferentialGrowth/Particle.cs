using Rhino.Geometry;
using Clipper2Lib;
using Extensions.Spatial;
using static Extensions.Util;

namespace Extensions.Simulations.DifferentialGrowth;

class Particle : IPositionable, IEquatable<Particle>
{
    Point3d _p;
    Vector3d _v;
    public Force Delta = new(Vector3d.Zero, 0);
    public Particle?[] Neighbours = new Particle?[2];
    readonly DifferentialGrowth _simulation;

    public int Index { get; }
    public ref Point3d Position => ref _p;
    public Vector3d Velocity => _v;

    public Particle(Point3d p, DifferentialGrowth simulation)
    {
        _simulation = simulation;
        _p = p;
        Index = _simulation.Particles.Count;
        _simulation.Particles.Add(this);
    }

    public void Forces()
    {
        Collision(100);
        Pull(1000);
    }

    public void Forces(Point3d pullPoint)
    {
        Collision(100);
        PullMesh(1000, pullPoint);
        KeepAngle(200);
    }

    void Collision(double weight)
    {
        var collided = _simulation.Search.GetClosests(this);
        double radius = _simulation.Radius;

        foreach (Particle collider in collided)
        {
            if (Neighbours[0] == collider || Neighbours[1] == collider)
                continue;

            Vector3d vector = collider._p - _p;
            double distance = vector.Length;

            if (distance == 0)
                continue;

            vector *= ((distance - radius) / distance) * 0.5 * weight;
            Delta.Add(vector, weight);
            collider.Delta.Add(-vector, weight);
        }
    }

    void PullMesh(double weight, Point3d closest)
    {
        Vector3d vector = closest - _p;
        Delta.Add(vector * weight, weight);
    }

    // Code lifted from https://github.com/Dan-Piker/K2Goals/blob/master/Angle.cs
    void KeepAngle(double weight)
    {
        var previous = Neighbours[0];
        var next = Neighbours[1];

        if (previous is null || next is null)
            return;

        double restAngle = 0;

        Point3d P0 = previous._p;
        Point3d P1 = _p;
        Point3d P2 = _p;
        Point3d P3 = next._p;

        Vector3d V01 = P1 - P0;
        Vector3d V23 = P3 - P2;
        double top = 2 * Math.Sin(Vector3d.VectorAngle(V01, V23) - restAngle);
        double Lc = (V01 + V23).Length;
        double Sa = top / (V01.Length * Lc);
        double Sb = top / (V23.Length * Lc);

        Vector3d Perp = Vector3d.CrossProduct(V01, V23);
        Vector3d ShearA = Vector3d.CrossProduct(V01, Perp);
        Vector3d ShearB = Vector3d.CrossProduct(Perp, V23);

        ShearA.Unitize();
        ShearB.Unitize();

        ShearA *= Sa;
        ShearB *= Sb;

        previous.Delta.Add(ShearA * weight, weight);
        Delta.Add(-ShearA * weight, weight);
        Delta.Add(ShearB * weight, weight);
        next.Delta.Add(-ShearB * weight, weight);
    }

    void Pull(double weight)
    {
        var region = _simulation.Region
            ?? throw new InvalidOperationException("A planar region is required to pull differential-growth particles.");

        bool isInside = Clipper.PointInPolygon(new Point64(_p.X / Tol, _p.Y / Tol), region) != PointInPolygonResult.IsOutside;

        if (!isInside)
        {
            var polyline = _simulation.Polyline
                ?? throw new InvalidOperationException("A planar region is required to pull differential-growth particles.");

            var closest = polyline.ClosestPoint(_p);
            Vector3d vector = closest - _p;
            Delta.Add(vector * weight, weight);
        }
    }

    public void Move()
    {
        Delta.Vector /= Delta.Weight;
        _v = Delta.Vector;
        _p += _v;

        Delta.SetZero();
    }

    bool IEquatable<Particle>.Equals(Particle? other)
    {
        return this == other;
    }
}
