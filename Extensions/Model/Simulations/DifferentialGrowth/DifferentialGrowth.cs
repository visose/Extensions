using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rhino.Geometry;
using Extensions.Model.Spatial;
using ClipperLib;
using static Extensions.Model.Util;

namespace Extensions.Model.Simulations.DifferentialGrowth
{
    public class DifferentialGrowth
    {
        internal List<Particle> Particles = new List<Particle>();
        internal List<Spring> Springs = new List<Spring>();
        internal BucketSearchDense3d Search;
        internal double Radius;

        public double Growth;
        public List<IntPoint> Region;
        public Mesh Mesh;
        public Polyline Polyline;
        public List<List<Polyline>> AllPolylines = new List<List<Polyline>>();
        public Polyline Boundary;
        public System.Diagnostics.Stopwatch Watch = new System.Diagnostics.Stopwatch();

        int _convergence;

        public DifferentialGrowth(IEnumerable<Polyline> polylines, double radius, int convergence, int maxIterations, Polyline region = null, Mesh mesh = null)
        {
            Region = region?.Select(p => new IntPoint(p.X / Tol, p.Y / Tol)).ToList();
            Polyline = region;
            Mesh = mesh;
            Radius = radius;
            Growth = radius * 0.5;
            _convergence = convergence;
            Boundary = mesh.GetNakedEdges().First();

            var box = Polyline != null ? Polyline.BoundingBox : mesh.GetBoundingBox(true);

            Search = new BucketSearchDense3d(box, Radius * 2);

            foreach (var polyline in polylines)
            {
                if (polyline == null) continue;
                var startPl = new Particle(polyline[0], this);

                int j = polyline.IsClosed ? -1 : 0;

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
                if (Math.Abs(lastLength - length) < 1.0) break;
                lastLength = length;
            }
        }

        void Grow()
        {
            Parallel.ForEach(Springs, spring =>
            {
                spring.RestLength = spring.Length + Growth;
            });
        }

        void Split()
        {
            for (int i = Springs.Count - 1; i >= 0; i--)
            {
                var spring = Springs[i];
                if (spring.Length > Radius)
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
                    var pullPoints = Mesh.PullPointsToMesh(Particles.Select(p => p.Position));
                    Parallel.For(0, Particles.Count, i => Particles[i].Forces(pullPoints[i]));
                }
                else
                {
                    Parallel.ForEach(Particles, p => p.Forces());
                }

                // Watch.Start();

                //Watch.Stop();
                Parallel.ForEach(Springs, s => s.Forces(10));

                Parallel.ForEach(Particles, p => p.Move());
                Parallel.ForEach(Springs, s => s.Update());

                totVel = 0;
                foreach (var particle in Particles)
                    totVel += particle.Velocity.SquareLength;
            } while (totVel > 0.001 && iterations-- >= 0);
        }

        public List<Polyline> GetPolylines()
        {
            var polylines = new List<Polyline>();
            Polyline pl = new Polyline
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
}
