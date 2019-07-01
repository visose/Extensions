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
        public Program Program { get; }
        int _axis = 6;
        double _width;
        double _height;
        double _zone;
        int _segments;
        bool _is3d;
        bool _isWorld;
        List<Contour> _contours;
        public IList<Mesh> ExtrudedContours { get; private set; }

        public ExtrusionVisualizer(Program program, double width, double height, double zone, bool isWorld, int segments, bool is3d = false)
        {
            Program = program;
            _width = width;
            _height = height;
            _zone = zone;
            _segments = segments;
            _is3d = is3d;
            _isWorld = isWorld;

            CreateContours(_axis);
        }

        public void Update()
        {
            double time = Program.CurrentSimulationTime;

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

                var programTarget = Program.CurrentSimulationTarget.ProgramTargets[0];
                var tcp = _isWorld ? programTarget.WorldPlane : programTarget.Plane;

                planes.Add(tcp);
                var mesh = CreateContourMesh(planes);
                extrudedContours.Add(mesh);
            }

            ExtrudedContours = extrudedContours;
        }

        Mesh CreateContourMesh(List<Plane> planes)
        {
            if (_is3d)
            {
                return Geometry.MeshPipe.MeshExtrusion3d(planes, _width, _height, _segments);
            }
            else
            {
                var pl = new Polyline(planes.Select(p => p.Origin));
                var mesh = Geometry.MeshPipe.MeshFlatPolyline(pl, _width, _height, _zone, _segments);
                return mesh;
            }
        }

        void CreateContours(int ex)
        {
            _contours = new List<Contour>();
            Contour contour = null;

            for (int i = 0; i < Program.Targets.Count; i++)
            {
                var cellTarget = Program.Targets[i];
                var nextTarget = i < Program.Targets.Count - 1 ? Program.Targets[i + 1] : cellTarget;

                var target = cellTarget.ProgramTargets[0];
                var plane = _isWorld ? target.WorldPlane : target.Plane;

                double current = target.Kinematics.Joints[ex];
                double next = i < Program.Targets.Count - 1 ? nextTarget.ProgramTargets[0].Kinematics.Joints[ex] : current;

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
                            contour.Mesh = CreateContourMesh(contour.Planes);
                            contour.Time.T1 = Program.Targets[i + 1].TotalTime;

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
                    contour.Mesh = CreateContourMesh(contour.Planes);
                    contour.Time.T1 = Program.Targets[i + 1].TotalTime;

                    _contours.Add(contour);
                    contour = null;
                }
            }
        }

        class Contour
        {
            public Interval Time;
            public List<Plane> Planes { get; set; } = new List<Plane>();
            public Mesh Mesh { get; set; }
        }
    }
}