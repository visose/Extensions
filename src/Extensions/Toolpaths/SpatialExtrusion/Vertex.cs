using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static System.Math;

namespace Extensions.Toolpaths.SpatialExtrusion;

class Vertex
{
    const double _tol = 0.01;

    readonly int _index;
    Point3d _point;
    Point3d _adjustedPoint;
    Vertex? _prev;
    Vertex? _post;
    Vector3d _prevVector;
    readonly List<Vertex> _vertices;
    readonly SpatialAttributes _att;
    readonly IReadOnlyList<GeometryBase> _environment;
    bool _isSegmentSupported;
    bool _isNodeSupported;
    bool _isDown;

    public Vertex(int index, Point3d point, List<Vertex> vertices, SpatialAttributes att, IReadOnlyList<GeometryBase> environment)
    {
        _index = index;
        _point = point;
        _vertices = vertices;
        _att = att;
        _environment = environment;
        vertices.Add(this);
    }

    public (Line, int) GetDisplay()
    {
        Line line = Line.Unset;
        if (_index > 0)
            line = new(Prev._adjustedPoint, _adjustedPoint);
        int type = (_isSegmentSupported ? 1 : 0) + (_isNodeSupported ? 2 : 0) + (_isDown ? 4 : 0);
        return (line, type);
    }

    Vertex Prev => _prev ?? throw new InvalidOperationException("Previous vertex has not been initialized.");

    Vertex Post => _post ?? throw new InvalidOperationException("Next vertex has not been initialized.");

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

        Point3d mid = Prev._point * 0.5 + _point * 0.5;
        _isSegmentSupported = IsSupported(mid);
        _isNodeSupported = IsSupported(_point);
    }

    bool IsSupported(Point3d point)
    {
        var meshes = _environment.OfType<Mesh>().ToArray();

        if (meshes.Length > 0)
        {
            Mesh environmentMesh = new();

            foreach (var mesh in meshes)
                environmentMesh.Append(mesh);

            Point3d closest = environmentMesh.ClosestPoint(point);

            if (point.DistanceTo(closest) < 0.1)
                return true;
        }

        var curves = _environment.OfType<Curve>();
        List<Polyline> polylines = [];

        foreach (var curve in curves)
        {
            if (!curve.TryGetPolyline(out Polyline polyline))
                continue;
            polylines.Add(polyline);
        }

        polylines.Add(new Polyline(_vertices.Select(v => v._point).Take(_index)));

        foreach (var polyline in polylines)
        {
            Point3d closest = polyline.ClosestPoint(point);
            Vector3d vector = point - closest;
            Vector3d horizontal = new(vector.X, vector.Y, 0);
            bool isSupported = (horizontal.Length < _att.Diameter * 0.5) && vector.Z >= 0 && vector.Z < _att.Diameter * 1.1;

            if (isSupported)
                return true;
        }

        return false;
    }

    void CalcDown()
    {
        if (_index == 0)
            _isDown = true;
        else if (_prevVector.Z > _tol)
            _isDown = true;
    }

    void CalcAdjusted()
    {
        _adjustedPoint = _point;

        if (_isNodeSupported)
            return;

        double angle = Vector3d.VectorAngle(-_prevVector, Vector3d.ZAxis);
        if (angle > PI * 0.5)
            angle = PI - angle;

        if (angle > _tol)
        {
            double t = angle / (PI * 0.5);
            double offsetAngle = _att.RotationOffset * t;

            var normal = Vector3d.CrossProduct(-_prevVector, Vector3d.ZAxis);
            _adjustedPoint.Transform(Transform.Rotation(-offsetAngle, normal, Prev._point));
        }

        _adjustedPoint.Z += _att.VerticalOffset;
    }

    Speed GetRobotSpeed() => (_index, _isSegmentSupported, _isDown) switch
    {
        (0, _, _) => _att.Plunge,
        (_, true, _) => _att.Fast,
        (_, false, false) => _att.Slow,
        (_, false, true) => _att.Medium
    };

    Command? GetWait() => (_isSegmentSupported, _post, _isNodeSupported) switch
    {
        (true, null, _) => _att.ShortWait,
        (true, { _isSegmentSupported: true }, _) => null,
        (true, { _isSegmentSupported: false }, _) => _att.ShortWait,
        (false, _, true) => _att.ShortWait,
        (false, _, false) => _att.LongWait
    };

    Command? GetExtrusionSpeed()
    {
        if (_index == _vertices.Count - 1)
            return _att.StopExtrusion;

        return (_isSegmentSupported, Post._isSegmentSupported, Post._isDown) switch
        {
            (false, true, _) => _att.FastExtrusion,
            (true, true, _) => null,
            (_, false, false) => _att.SlowExtrusion,
            (_, _, true) => _att.MediumExtrusion
        };
    }

    public List<Target> GetTargets(bool hasToWait)
    {
        List<Target> targets = [];

        var speed = GetRobotSpeed();
        var wait = GetWait();
        var extrusion = GetExtrusionSpeed();

        var reference = _att.ReferenceTarget;
        Plane plane = new(_adjustedPoint, reference.Plane.XAxis, reference.Plane.YAxis);
        var target = reference.WithPlaneSpeedCommand(plane, speed, RobotTargetUtil.CombineCommands(wait, extrusion));

        if (_index == 0)
        {
            Plane approachPlane = new(target.Plane.Origin + Vector3d.ZAxis * _att.DistancePlunge, target.Plane.XAxis, target.Plane.YAxis);
            var approachTarget = target.WithPlaneSpeedCommand(approachPlane, _att.Approach, hasToWait ? new Stop() : null);
            targets.Add(approachTarget);
        }

        if (!_isDown && !_isSegmentSupported)
        {
            var vector = _prevVector * _att.DistanceAhead;
            var point = _adjustedPoint + vector;

            Plane aheadPlane = new(point, target.Plane.XAxis, target.Plane.YAxis);
            var aheadTarget = target.WithPlaneSpeedCommand(aheadPlane, target.Speed, _att.AheadCommand);
            targets.Add(aheadTarget);
        }

        if (_index > 0 && _isDown && !_isSegmentSupported)
        {
            var projected = -new Vector3d(_prevVector.X, _prevVector.Y, 0);
            var length = projected.Length;
            ArgumentOutOfRangeException.ThrowIfZero(length, nameof(length));

            if (_att.DistanceHorizontal > length)
                throw new InvalidOperationException($"Segment {_index} is shorter than the configured horizontal distance ({_att.DistanceHorizontal}).");

            var vector = projected * (_att.DistanceHorizontal / length);
            var point = Prev._adjustedPoint + vector;

            Plane horizontalPlane = new(point, target.Plane.XAxis, target.Plane.YAxis);
            var horizontalTarget = target.WithPlaneSpeedCommand(horizontalPlane, target.Speed, null);
            targets.Add(horizontalTarget);
        }

        targets.Add(target);

        if (_index == _vertices.Count - 1)
        {
            Plane approachPlane = new(target.Plane.Origin + Vector3d.ZAxis * _att.DistancePlunge, target.Plane.XAxis, target.Plane.YAxis);
            var approachTarget = target.WithPlaneSpeedCommand(approachPlane, _att.Plunge, null);
            targets.Add(approachTarget);
        }

        return targets;
    }
}
