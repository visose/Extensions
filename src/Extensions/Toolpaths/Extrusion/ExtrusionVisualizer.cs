using Rhino.Geometry;
using Robots;
using static Extensions.Util;

namespace Extensions.Toolpaths.Extrusion;

public class ExtrusionVisualizer
{
    public Program Program { get; }

    readonly int _axis = 6;
    readonly double _width;
    readonly double _height;
    readonly double _zone;
    readonly int _segments;
    readonly bool _is3d;
    List<Contour> _contours;
    public IList<Mesh> ExtrudedContours { get; private set; }

    public ExtrusionVisualizer(Program program, double width, double height, double zone, int segments, bool is3d = false, bool reverse = false)
    {
        Program = program;
        _width = width;
        _height = height;
        _zone = zone;
        _segments = segments;
        _is3d = is3d;

        CreateContours(_axis, reverse);
    }

    public void Update()
    {
        double time = Program.CurrentSimulationPose.CurrentTime;

        var extrudedContours = _contours.Where(x => x.Time.T1 <= time).Select(x => x.Mesh).ToList();
        var currentContour = _contours.Find(x => x.Time.T0 < time && x.Time.T1 > time);

        if (currentContour != null)
        {
            double t = currentContour.Time.NormalizedParameterAt(time);
            var pl = new Polyline(currentContour.Planes.Select(p => p.Origin));
            var currentLength = t * pl.Length;

            var planes = new List<Plane>
            {
                currentContour.Planes[0]
            };

            var lines = pl.GetSegments();
            double current = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                current += lines[i].Length;

                if (current > currentLength)
                    break;

                planes.Add(currentContour.Planes[i + 1]);
            }

            var tcp = Program.CurrentSimulationPose.GetLastPlane(0);

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

    void CreateContours(int ex, bool reverse)
    {
        _contours = new List<Contour>();
        Contour contour = null;

        for (int i = 0; i < Program.Targets.Count; i++)
        {
            var cellTarget = Program.Targets[i];
            var nextTarget = i < Program.Targets.Count - 1 ? Program.Targets[i + 1] : cellTarget;

            var target = cellTarget.ProgramTargets[0];
            var plane = target.WorldPlane;

            double current = target.Kinematics.Joints[ex];
            double next = i < Program.Targets.Count - 1 ? nextTarget.ProgramTargets[0].Kinematics.Joints[ex] : current;

            double delta = reverse ? current - next : next - current;

            bool isExtruding = delta > UnitTol;
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
