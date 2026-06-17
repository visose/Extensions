using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static System.Math;
using CustomCommand = Robots.Commands.Custom;

namespace Extensions.Toolpaths.Extrusion;

readonly record struct SimpleTarget(Plane Location, double Length);

public class ExternalExtrusionToolpath : TargetListToolpath
{
    readonly List<int> _subPrograms = [];

    public IReadOnlyList<int> SubPrograms => _subPrograms;

    readonly ExtrusionAttributes _att;
    readonly double _extrusionFactor;
    readonly double _suckBack;
    readonly double _startDistance;
    readonly double _loopDistance;

    public ExternalExtrusionToolpath(IReadOnlyList<Polyline> polylines, ExtrusionAttributes attributes, double extrusionFactor, double suckBack, double startDistance, double loopDistance)
    {
        ArgumentOutOfRangeException.ThrowIfZero(polylines.Count, nameof(polylines));

        _att = attributes;
        _extrusionFactor = extrusionFactor;
        _suckBack = -suckBack;
        _startDistance = startDistance;
        _loopDistance = loopDistance;

        var robotPosition = Point3d.Origin;
        robotPosition.Transform(Transform.PlaneToPlane(_att.Frame.Plane, Plane.WorldXY));

        var paths = polylines.Select(p => ToTargets(p, robotPosition)).ToList();
        CreateTargets(paths);
    }

    public ExternalExtrusionToolpath(IReadOnlyList<IReadOnlyList<Plane>> locations, IReadOnlyList<IReadOnlyList<double>> lengths, ExtrusionAttributes attributes, double extrusionFactor, double suckBack, double startDistance, double loopDistance)
    {
        _att = attributes;
        _extrusionFactor = extrusionFactor;
        _suckBack = -suckBack;
        _startDistance = startDistance;
        _loopDistance = loopDistance;

        List<IReadOnlyList<SimpleTarget>> paths = new(locations.Count);

        ArgumentOutOfRangeException.ThrowIfNotEqual(lengths.Count, locations.Count, nameof(lengths));

        ArgumentOutOfRangeException.ThrowIfZero(locations.Count, nameof(locations));

        for (int i = 0; i < locations.Count; i++)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(lengths[i].Count, locations[i].Count, nameof(lengths));

            int pathCount = locations[i].Count;

            ArgumentOutOfRangeException.ThrowIfZero(pathCount, nameof(locations));

            List<SimpleTarget> path = new(pathCount);

            for (int j = 0; j < pathCount; j++)
            {
                path.Add(new(locations[i][j], lengths[i][j]));
            }

            paths.Add(path);
        }

        CreateTargets(paths);
    }

    List<SimpleTarget> ToTargets(Polyline path, Point3d robotPosition)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(path.Count, 2, nameof(path));

        List<SimpleTarget> targets = new(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            var pos = path[i];
            Plane plane = new(pos, Vector3d.ZAxis);
            double angle = Vector3d.VectorAngle(robotPosition - pos, plane.XAxis, plane);
            plane.Rotate(-angle, plane.Normal);

            double external = 0;

            if (i > 0)
            {
                var prev = path[i - 1];
                double length = prev.DistanceTo(pos);
                external = ExternalValue(length);
            }

            targets.Add(new SimpleTarget { Location = plane, Length = external });
        }

        return targets;
    }

    double ExternalValue(double length)
    {
        return ((PI * (_att.BeadWidth * 0.5) * (_att.LayerHeight * 0.5)) * length);
    }

    void CreateTargets(IReadOnlyList<IReadOnlyList<SimpleTarget>> paths)
    {
        double totalDistance = 0;
        var externalCustom = new[] { "motorValue" };

        AddTarget(HomeStart());

        foreach (var path in paths)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(path.Count, 2, nameof(paths));

            var first = path[0].Location;
            var last = path[path.Count - 1].Location;

            var firstSafe = first; firstSafe.Origin += firstSafe.Normal * _att.SafeZOffset;

            AddTarget(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone, 0));

            AddTarget(CreateTarget(first, _att.ApproachSpeed, _att.ApproachZone, 0));
            AddTarget(CreateTarget(first, _att.ApproachSpeed, _att.ApproachZone, _startDistance));

            for (int i = 1; i < path.Count; i++)
            {
                Plane position = path[i].Location;
                double segmentLength = path[i].Length;

                var zone = i != path.Count - 1 ? _att.ExtrusionZone : _att.ApproachZone;
                AddTarget(CreateTarget(position, _att.ExtrusionSpeed, zone, segmentLength));
            }

            AddTarget(CreateTarget(last, _att.ExtrusionSpeed, _att.ApproachZone, _suckBack));

            var lastOffset = last; lastOffset.Origin += lastOffset.Normal * (_att.SafeZOffset + _att.LayerHeight);
            AddTarget(CreateTarget(lastOffset, _att.ApproachSpeed, _att.SafeZone, 0));

            _subPrograms.Add(TargetCount);
        }

        AddTarget(HomeEnd());
        _subPrograms.RemoveAt(_subPrograms.Count - 1);

        Target CreateTarget(Plane location, Speed speed, Zone zone, double externalDistance)
        {
            var frame = _att.Frame;
            var tool = _att.Tool;

            totalDistance += externalDistance * _extrusionFactor;

            Command? command = null;

            if (externalDistance != 0)
            {
                string sign = externalDistance < 0 ? "+" : "-";
                string code = $"motorValue:=motorValue{sign}{Abs(externalDistance):0.000}*extrusionFactor;";
                CustomCommand externalCommand = new($"SetExternal{TargetCount}", Manufacturers.ABB, code)
                {
                    RunBefore = true
                };
                command = externalCommand;
            }

            CartesianTarget target = new(location, null, Motions.Linear, tool, speed, zone, command, frame, [totalDistance])
            {
                ExternalCustom = externalCustom
            };
            return target;
        }

        Target HomeStart()
        {
            var externalValue = ExternalValue(_loopDistance);

            string declaration = $@"VAR num motorValue:= 0;
PERS num extrusionFactor:={_extrusionFactor:0.000};
VAR robtarget current;
VAR num choice:=0;
";
            string initCode = $@"current:= CRobT(\Tool:= {_att.Tool.Name} \WObj:= {_att.Frame.Name});
EOffsSet current.extax;";

            string testCode = $@"TPReadFK choice,""Choose mode"",stEmpty,stEmpty,stEmpty,""Program"",""Test"";
current:= CRobT(\Tool:= {_att.Tool.Name} \WObj:= {_att.Frame.Name});
WHILE choice = 5 DO
    motorValue:= motorValue - {externalValue:0.00}*extrusionFactor;
    current.extax.eax_a:= motorValue;
    MoveL Offs(current,0,{_loopDistance},0),{_att.ExtrusionSpeed.Name},{_att.ExtrusionZone.Name},{_att.Tool.Name} \WObj:= {_att.Frame.Name};
    motorValue:= motorValue - {externalValue:0.00}*extrusionFactor;
    current.extax.eax_a:= motorValue;
    MoveL Offs(current,0,0,0),{_att.ExtrusionSpeed.Name},{_att.ExtrusionZone.Name},{_att.Tool.Name} \WObj:= {_att.Frame.Name};
ENDWHILE";

            CustomCommand initCommand = new("Init", Manufacturers.ABB, initCode, declaration)
            {
                RunBefore = true
            };
            CustomCommand testCommand = new("Test", Manufacturers.ABB, testCode);

            Group command = new([initCommand, testCommand]);
            JointTarget home = new(_att.Home, _att.Tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame, [totalDistance])
            {
                ExternalCustom = externalCustom
            };
            return home;
        }

        Target HomeEnd()
        {
            Group command = new([new Message("Se acabó."), new Stop()]);
            JointTarget home = new(_att.Home, _att.Tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame, [totalDistance])
            {
                ExternalCustom = externalCustom
            };
            return home;
        }
    }
}
