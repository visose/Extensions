using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using Extensions.Model.Spatial;
using ClipperLib;
using static Extensions.Model.Util;

namespace Extensions.Model.Simulations.DifferentialGrowth
{
    public class Particle : IPositionable, IEquatable<Particle>
    {
        Point3d _p;
        //Point3d _pPrev;
        Vector3d _v;
        //Vector3d _vPrev;
        public Force delta = new Force(Vector3d.Zero, 0);
        public List<Force> deltas = new List<Force>();
        public List<Particle> neighbours = new List<Particle>(2);
        DifferentialGrowth _simulation;

        public Stopwatch Watch = new Stopwatch();

        public Point3d Position { get { return _p; } }
        public Vector3d Velocity { get { return _v; } }

        public Particle(Point3d p, DifferentialGrowth simulation, bool insert = true)
        {
            _simulation = simulation;
            _p = p;
            if (insert) simulation.Particles.Add(this);
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

            foreach (Particle collider in collided)
            {
                if (neighbours.Contains(collider)) continue;

                Vector3d vector = collider._p - _p;
                double distance = vector.Length;
                vector *= ((distance - _simulation.Radius * 2) / distance) * 0.5;
                this.delta.Add(vector * weight, weight);
            }
        }

        void PullMesh(double weight, Point3d pullPoint)
        {
            var closest = pullPoint;
            Vector3d vector = closest - _p;
            this.delta.Add(vector * weight, weight);
        }

        void PushBoundary(double weight)
        {
            var closest = _simulation.Boundary.ClosestPoint(_p);
            Vector3d vector = closest - _p;
            double distance = vector.Length;
            if (distance < _simulation.Radius * 2)
            {
                vector *= ((distance - _simulation.Radius * 2) / distance) * 0.5;
                this.delta.Add(vector * weight, weight);
            }
        }


        // Code lifted from https://github.com/Dan-Piker/K2Goals/blob/master/Angle.cs

        void KeepAngle(double weight)
        {
            /// <param name="RA">Rest Angle.</param>
            /// <param name="P0">Start of the first line segment.</param>
            /// <param name="P1">End of the first line segment. This can be identical to P2 if the line segments are connected.</param>
            /// <param name="P2">Start of the second line segment. This can be identical to P1 if the line segments are connected.</param>
            /// <param name="P3">End of the second line segment.</param>
            /// 
            if (this.neighbours.Count != 2) return;

            double restAngle = 0;

            Point3d P0 = this.neighbours[0]._p;
            Point3d P1 = _p;
            Point3d P2 = _p;
            Point3d P3 = this.neighbours[1]._p;

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

            this.neighbours[0].delta.Add(ShearA * weight, weight);
            this.delta.Add(-ShearA * weight, weight);
            this.delta.Add(ShearB * weight, weight);
            this.neighbours[1].delta.Add(-ShearB * weight, weight);
        }

        void Pull(double weight)
        {
            bool isInside = Clipper.PointInPolygon(new IntPoint(_p.X / Tol, _p.Y / Tol), _simulation.Region) != 0;

            if (!isInside)
            {
                var closest = _simulation.Polyline.ClosestPoint(_p);
                Vector3d vector = closest - _p;
                this.delta.Add(vector * weight, weight);
            }
        }

        public void Move()
        {
            foreach (var subDelta in deltas)
                delta.Add(subDelta);

            delta.vector /= delta.weight;
            _v = delta.vector;
            _p = _p + 1.0 * _v;

            delta.SetZero();
            deltas.Clear();
        }

        bool IEquatable<Particle>.Equals(Particle other)
        {
            return this == other;
        }
    }
}