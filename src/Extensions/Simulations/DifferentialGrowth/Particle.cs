using System.Diagnostics;
using Rhino.Geometry;
using Extensions.Spatial;
using ClipperLib;
using static Extensions.Util;

namespace Extensions.Simulations.DifferentialGrowth;

public class Particle : IPositionable, IEquatable<Particle>
{
    Point3d _p;
    Vector3d _v;
    public Force Delta = new(Vector3d.Zero, 0);
    public List<Force> deltas = new();
    public Particle[] Neighbours = new Particle[2];
    readonly DifferentialGrowth _simulation;

    public Stopwatch Watch = new();


    //public Point3d Position
    //{
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    get { ref return _p; }
    //}

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
        //   PushBoundary(100);
    }

    void Collision(double weight)
    {
        var collided = _simulation.Search.GetClosests(this);
        double radius = _simulation.Radius;

        foreach (Particle collider in collided)
        {
            if (Neighbours[0] == collider || Neighbours[1] == collider)
                continue;

            //if (Neighbours.Contains(collider)) continue;

            Vector3d vector = collider._p - _p;
            double distance = vector.Length;
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

    //void PushBoundary(double weight)
    //{
    //    var closest = _simulation.Boundary.ClosestPoint(_p);
    //    Vector3d vector = closest - _p;
    //    double distance = vector.Length;
    //    if (distance < _simulation.Radius * 2)
    //    {
    //        vector *= ((distance - _simulation.Radius) / distance) * 0.5;
    //        Delta.Add(vector * weight, weight);
    //    }
    //}

    // Code lifted from https://github.com/Dan-Piker/K2Goals/blob/master/Angle.cs
    void KeepAngle(double weight)
    {
        /// <param name="RA">Rest Angle.</param>
        /// <param name="P0">Start of the first line segment.</param>
        /// <param name="P1">End of the first line segment. This can be identical to P2 if the line segments are connected.</param>
        /// <param name="P2">Start of the second line segment. This can be identical to P1 if the line segments are connected.</param>
        /// <param name="P3">End of the second line segment.</param>
        /// 

        if (Neighbours[0] == null || Neighbours[1] == null) return;
        //if (this.Neighbours.Length != 2) return;

        double restAngle = 0;

        Point3d P0 = Neighbours[0]._p;
        Point3d P1 = _p;
        Point3d P2 = _p;
        Point3d P3 = Neighbours[1]._p;

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

        Neighbours[0].Delta.Add(ShearA * weight, weight);
        Delta.Add(-ShearA * weight, weight);
        Delta.Add(ShearB * weight, weight);
        Neighbours[1].Delta.Add(-ShearB * weight, weight);
    }

    void Pull(double weight)
    {
        bool isInside = Clipper.PointInPolygon(new IntPoint(_p.X / Tol, _p.Y / Tol), _simulation.Region) != 0;

        if (!isInside)
        {
            var closest = _simulation.Polyline.ClosestPoint(_p);
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

    bool IEquatable<Particle>.Equals(Particle other)
    {
        return this == other;
    }
}
