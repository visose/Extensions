using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static Extensions.Model.Util;

namespace Extensions.Model.Toolpaths.Extrusion
{
    public class ExtrusionVisualizer
    {
        Program _program;
        ExtrusionAttributes _att;
        int _axis = 6;
        double _height;
        double _width;
        int _segments;
        List<Contour> _contours;

        public Program Program => _program;
        public IList<Mesh> ExtrudedContours { get; private set; }

        public ExtrusionVisualizer(Program program, ExtrusionAttributes attributes, bool isWorld, int segments)
        {
            _att = attributes;
            _segments = segments;

            _program = program;
            _height = _att.LayerHeight;
            _width = _att.BeadWidth;
            segments = _segments;

            CreateContours(_axis, isWorld);
        }

        public void Update()
        {
            double time = _program.CurrentSimulationTime;

            var extrudedContours = _contours.Where(x => x.Time.T1 <= time).Select(x => x.Mesh).ToList();
            var currentContour = _contours.Find(x => x.Time.T0 < time && x.Time.T1 > time);

            if (currentContour != null)
            {
                double t = currentContour.Time.NormalizedParameterAt(time);
                var pl = new Polyline(currentContour.Planes.Select(p => p.Origin));
                var currentLength = t * pl.Length;

                var planes = new List<Plane>();
                planes.Add(currentContour.Planes[0]);

                var lines = pl.GetSegments();
                double current = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    current += lines[i].Length;

                    if (current > currentLength)
                        break;

                    planes.Add(currentContour.Planes[i + 1]);
                }

                planes.Add(_program.CurrentSimulationTarget.ProgramTargets[0].Plane);
                //var mesh = Geometry.MeshPipe.MeshPlanes(planes, _width, _height, _segments);
                var mesh = Geometry.MeshPipe.MeshFlatPolyline(new Polyline(planes.Select(p => p.Origin)), _width, _height, _att.ExtrusionZone.Distance, _segments);
                extrudedContours.Add(mesh);
            }

            ExtrudedContours = extrudedContours;
        }

        class Contour
        {
            public Interval Time;
            public List<Plane> Planes { get; set; }
            public Mesh Mesh { get; set; }

            public Contour()
            {
                Planes = new List<Plane>();
            }
        }

        void CreateContours(int ex, bool isWorld)
        {
            _contours = new List<Contour>();
            Contour contour = null;

            for (int i = 0; i < _program.Targets.Count; i++)
            {
                var cellTarget = _program.Targets[i];
                var nextTarget = i < _program.Targets.Count - 1 ? _program.Targets[i + 1] : cellTarget;

                var target = cellTarget.ProgramTargets[0];
                var plane = isWorld ? target.WorldPlane : target.Plane;

                double current = target.Kinematics.Joints[ex];
                double next = i < _program.Targets.Count - 1 ? nextTarget.ProgramTargets[0].Kinematics.Joints[ex] : current;

                bool isExtruding = next - current > UnitTol;
                //bool isExtruding = next > 0.01;

                if (isExtruding)
                {
                    if (contour == null)
                    {
                        contour = new Contour();
                        contour.Time.T0 = cellTarget.TotalTime;
                    }

                    bool add = true;

                    if (contour.Planes.Count > 0)
                    {
                        var distance = contour.Planes[contour.Planes.Count - 1].Origin.DistanceToSquared(plane.Origin);
                        add = distance > Tol * Tol;
                    }

                    if (add)
                    {
                        contour.Planes.Add(plane);

                        if (contour.Planes.Count == 100)
                        {
                            var pl = new Polyline(contour.Planes.Select(p => p.Origin));
                            contour.Mesh = Geometry.MeshPipe.MeshFlatPolyline(pl, _width, _height, _att.ExtrusionZone.Distance, _segments);
                            contour.Time.T1 = _program.Targets[i + 1].TotalTime;

                            _contours.Add(contour);

                            contour = new Contour();
                            contour.Planes.Add(plane);
                            contour.Time.T0 = cellTarget.TotalTime;
                        }
                    }
                }
                else if (contour != null)
                {
                    contour.Planes.Add(plane);
                    //contour.Mesh = Geometry.MeshPipe.MeshPlanes(contour.Planes, _width, _height, _segments);
                    var pl = new Polyline(contour.Planes.Select(p => p.Origin));
                    contour.Mesh = Geometry.MeshPipe.MeshFlatPolyline(pl, _width, _height, _att.ExtrusionZone.Distance, _segments);
                    contour.Time.T1 = _program.Targets[i + 1].TotalTime;

                    _contours.Add(contour);
                    contour = null;
                }
            }
        }
    }
}