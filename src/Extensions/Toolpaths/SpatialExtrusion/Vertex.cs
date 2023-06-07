using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static System.Math;


namespace Extensions.Toolpaths.SpatialExtrusion;

class Vertex
{
    private const double _tol = 0.01;

    private readonly int _index;
    private Point3d _point;
    private Point3d _adjustedPoint;
    private Vertex _prev;
    private Vertex _post;
    private Vector3d _prevVector;
    private readonly List<Vertex> _vertices;
    private readonly SpatialAttributes _att;
    private bool _isSegmentSupported;
    private bool _isNodeSupported;
    private bool _isDown;

    public Vertex(int index, Point3d point, List<Vertex> vertices, SpatialAttributes att)
    {
        _index = index;
        _point = point;
        _vertices = vertices;
        _att = att;
        vertices.Add(this);
    }

    public (Line, int) GetDisplay()
    {
        Line line = Line.Unset;
        if (_index > 0) line = new Line(_prev._adjustedPoint, _adjustedPoint);
        int type = (_isSegmentSupported ? 1 : 0) + (_isNodeSupported ? 2 : 0) + (_isDown ? 4 : 0);
        return (line, type);
    }

    public void Initialize()
    {
        CalcNeighbours();
        CalcSupported();
        CalcDown();
        CalcAdjusted();
    }

    void CalcNeighbours()
    {
        if (_index > 0)
        {
            _prev = _vertices[_index - 1];
            _prevVector = _prev._point - _point;
        }

        if (_index < _vertices.Count - 1)
        {
            _post = _vertices[_index + 1];
        }
    }

    void CalcSupported()
    {
        if (_index == 0)
        {
            _isSegmentSupported = false;
            _isNodeSupported = true;
            return;
        }

        Point3d mid = _prev._point * 0.5 + _point * 0.5;
        _isSegmentSupported = IsSupported(mid);
        _isNodeSupported = IsSupported(_point);
    }

    bool IsSupported(Point3d point)
    {
        {
            var meshes = _att.Environment.OfType<Mesh>();

            if (meshes.Count() > 0)
            {
                var environmentMesh = new Mesh();
                foreach (var mesh in meshes)
                    environmentMesh.Append(mesh);

                Point3d closest = environmentMesh.ClosestPoint(point);
                if (point.DistanceTo(closest) < 0.1) return true;
            }
        }

        var curves = _att.Environment.OfType<Curve>();
        var polylines = new List<Polyline>();

        foreach (var curve in curves)
        {
            if (!curve.TryGetPolyline(out Polyline polyline)) continue;
            polylines.Add(polyline);
        }

        polylines.Add(new Polyline(_vertices.Select(v => v._point).Take(_index)));

        foreach (var polyline in polylines)
        {
            Point3d closest = polyline.ClosestPoint(point);
            Vector3d vector = point - closest;
            Vector3d horizontal = new(vector.X, vector.Y, 0);
            bool isSupported = (horizontal.Length < _att.Diameter * 0.5) && vector.Z >= 0 && vector.Z < _att.Diameter * 1.1;
            if (isSupported) return true;
        }

        return false;
    }

    void CalcDown()
    {
        if (_index == 0) _isDown = true;
        else if (_prevVector.Z > _tol) _isDown = true;
    }

    void CalcAdjusted()
    {
        _adjustedPoint = _point;
        if (_isNodeSupported) return;

        double angle = Vector3d.VectorAngle(-_prevVector, Vector3d.ZAxis);
        if (angle > PI * 0.5) angle = PI - angle;

        if (angle > _tol)
        {
            double t = angle / (PI * 0.5);
            double offsetAngle = _att.RotationOffset * t;

            var normal = Vector3d.CrossProduct(-_prevVector, Vector3d.ZAxis);
            _adjustedPoint.Transform(Transform.Rotation(-offsetAngle, normal, _prev._point));
        }

        _adjustedPoint.Z += _att.VerticalOffset;
    }

    Speed GetRobotSpeed()
    {
        if (_index == 0) return _att.Plunge;
        if (_isSegmentSupported) return _att.Fast;
        if (!_isDown) return _att.Slow;
        if (_isDown) return _att.Medium;

        throw new Exception($" Speed case in node {_index} not considered.");
    }

    Command GetWait()
    {
        if (_isSegmentSupported && _post == null) return _att.ShortWait;
        if (_isSegmentSupported && _post._isSegmentSupported) return null;
        if (_isSegmentSupported && !_post._isSegmentSupported) return _att.ShortWait;
        if (_isNodeSupported) return _att.ShortWait;
        if (!_isNodeSupported) return _att.LongWait;

        throw new Exception($" Wait case in node {_index} not considered.");
    }

    Command GetExtrusionSpeed()
    {
        if (_index == _vertices.Count - 1) return _att.StopExtrusion;
        if (!_isSegmentSupported && _post._isSegmentSupported) return _att.FastExtrusion;
        if (_isSegmentSupported && _post._isSegmentSupported) return null;
        if (!_post._isSegmentSupported && !_post._isDown) return _att.SlowExtrusion;
        if (_post._isDown) return _att.MediumExtrusion;

        throw new Exception($" ExtrusionSpeed case in node {_index} not considered.");
    }

    public List<Target> GetTargets(bool hasToWait)
    {
        var targets = new List<Target>();

        var speed = GetRobotSpeed();
        var wait = GetWait();
        var extrusion = GetExtrusionSpeed();

        var target = _att.ReferenceTarget.ShallowClone() as CartesianTarget;
        target.Plane = new Plane(_adjustedPoint, target.Plane.XAxis, target.Plane.YAxis);
        target.Speed = speed;
        target.Command = new Group() { wait, extrusion };

        if (_index == 0)
        {
            var approachTarget = target.ShallowClone() as CartesianTarget;
            approachTarget.Plane = new Plane(target.Plane.Origin + Vector3d.ZAxis * _att.DistancePlunge, target.Plane.XAxis, target.Plane.YAxis);
            approachTarget.Speed = _att.Approach;
            approachTarget.Command = hasToWait ? new Stop() : null;
            targets.Add(approachTarget);
        }


        if (!_isDown && !_isSegmentSupported)
        {
            var vector = _prevVector * _att.DistanceAhead;
            var point = _adjustedPoint + vector;

            var aheadTarget = target.ShallowClone() as CartesianTarget;
            aheadTarget.Plane = new Plane(point, target.Plane.XAxis, target.Plane.YAxis);
            aheadTarget.Command = _att.AheadCommand;
            targets.Add(aheadTarget);
        }

        if (_index > 0 && _isDown && !_isSegmentSupported)
        {
            var projected = -new Vector3d(_prevVector.X, _prevVector.Y, 0);
            var length = projected.Length;
            if (_att.DistanceHorizontal > length) throw new Exception($" Segment {_index} is shorter than horizontal distance value of {_att.DistanceHorizontal}");
            var vector = projected * (_att.DistanceHorizontal / length);
            var point = _prev._adjustedPoint + vector;

            var horizontalTarget = target.ShallowClone() as CartesianTarget;
            horizontalTarget.Plane = new Plane(point, target.Plane.XAxis, target.Plane.YAxis);
            horizontalTarget.Command = null;
            targets.Add(horizontalTarget);
        }

        targets.Add(target);

        if (_index == _vertices.Count - 1)
        {
            var approachTarget = target.ShallowClone() as CartesianTarget;
            approachTarget.Plane = new Plane(target.Plane.Origin + Vector3d.ZAxis * _att.DistancePlunge, target.Plane.XAxis, target.Plane.YAxis);
            approachTarget.Speed = _att.Plunge;
            approachTarget.Command = null;
            targets.Add(approachTarget);
        }

        return targets;
    }
}
