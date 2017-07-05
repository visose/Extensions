using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using static System.Math;

namespace Extensions.Toolpaths
{
    internal class SpatialExtrusion
    {
        public List<Target> Targets { get; private set; } = new List<Target>();
        public IEnumerable<(Line segment, int type)> Display { get; private set; }

        public SpatialExtrusion(Polyline polyline, SpatialAttributes attributes)
        {
            CreateTargets(polyline, attributes);
        }

        void CreateTargets(Polyline polyline, SpatialAttributes attributes)
        {
            var vertices = new List<Vertex>(polyline.Count);

            for (int i = 0; i < polyline.Count; i++)
                new Vertex(i, polyline[i], vertices, attributes);

            foreach (var vertex in vertices) vertex.Initialize();
            foreach (var vertex in vertices) Targets.AddRange(vertex.GetTargets());

            Display = vertices.Select(v => v.GetDisplay());
        }
    }
}
