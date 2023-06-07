using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static System.Math;

namespace Extensions.Toolpaths.Extrusion;

struct SimpleTarget
{
    public Plane Location;
    public double Length;
}

public class ExternalExtrusionToolpath : SimpleToolpath
{
    public List<int> SubPrograms { get; set; } = new List<int>();

    readonly ExtrusionAttributes _att;
    readonly double _extrusionFactor;
    readonly double _suckBack;
    readonly double _startDistance;
    readonly double _loopDistance;

    public ExternalExtrusionToolpath(IList<Polyline> polylines, ExtrusionAttributes attributes, double extrusionFactor, double suckBack, double startDistance, double loopDistance)
    {
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

    public ExternalExtrusionToolpath(List<List<Plane>> locations, List<List<double>> lengths, ExtrusionAttributes attributes, double extrusionFactor, double suckBack, double startDistance, double loopDistance)
    {
        _att = attributes;
        _extrusionFactor = extrusionFactor;
        _suckBack = -suckBack;
        _startDistance = startDistance;
        _loopDistance = loopDistance;

        var paths = new List<List<SimpleTarget>>(locations.Count);

        if (locations.Count != lengths.Count)
            throw new ArgumentException("Number of paths in locations and lengths don't match.");

        if (locations.Count == 0)
            throw new ArgumentException("There should be more than one path.");

        for (int i = 0; i < locations.Count; i++)
        {
            if (locations[i].Count != lengths[i].Count)
                throw new ArgumentException($"Locations and lengths in path {i} don't match.");

            int pathCount = locations[0].Count;

            var path = new List<SimpleTarget>(pathCount);

            for (int j = 0; j < locations[0].Count; j++)
            {
                path.Add(new SimpleTarget() { Location = locations[i][j], Length = lengths[i][j] });
            }

            paths.Add(path);
        }

        CreateTargets(paths);
    }

    List<SimpleTarget> ToTargets(Polyline path, Point3d robotPosition)
    {
        var targets = new List<SimpleTarget>(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            var pos = path[i];
            var plane = new Plane(pos, Vector3d.ZAxis);
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

    void CreateTargets(List<List<SimpleTarget>> paths)
    {
        double totalDistance = 0;
        var externalCustom = new[] { "motorValue" };

        _targets.Add(HomeStart());

        foreach (var path in paths)
        {
            var first = path[0].Location;
            var last = path[path.Count - 1].Location;

            var firstSafe = first; firstSafe.Origin += firstSafe.Normal * _att.SafeZOffset;

            _targets.Add(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone, 0));

            _targets.Add(CreateTarget(first, _att.ApproachSpeed, _att.ApproachZone, 0));
            _targets.Add(CreateTarget(first, _att.ApproachSpeed, _att.ApproachZone, _startDistance));

            for (int i = 1; i < path.Count; i++)
            {
                //double segmentLength = path[i - 1].DistanceTo(path[i]);
                Plane position = path[i].Location;
                double segmentLength = path[i].Length;

                var zone = i != path.Count - 1 ? _att.ExtrusionZone : _att.ApproachZone;
                _targets.Add(CreateTarget(position, _att.ExtrusionSpeed, zone, segmentLength));
            }

            _targets.Add(CreateTarget(last, _att.ExtrusionSpeed, _att.ApproachZone, _suckBack));

            var lastOffset = last; lastOffset.Origin += lastOffset.Normal * (_att.SafeZOffset + _att.LayerHeight);
            _targets.Add(CreateTarget(lastOffset, _att.ApproachSpeed, _att.SafeZone, 0));

            SubPrograms.Add(_targets.Count);
        }

        _targets.Add(HomeEnd());
        SubPrograms.RemoveAt(SubPrograms.Count - 1);

        Target CreateTarget(Plane location, Speed speed, Zone zone, double externalDistance)
        {
            var frame = _att.Frame;
            var tool = _att.Tool;

            totalDistance += externalDistance * _extrusionFactor;

            Command command = null;

            if (externalDistance != 0)
            {
                string sign = externalDistance < 0 ? "+" : "-";
                string code = $"motorValue:=motorValue{sign}{Abs(externalDistance):0.000}*extrusionFactor;";
                var externalCommand = new Robots.Commands.Custom($"SetExternal{_targets.Count}", command: code)
                {
                    RunBefore = true
                };
                command = externalCommand;
            }

            var target = new CartesianTarget(location, null, Motions.Linear, tool, speed, zone, command, frame, new[] { totalDistance })
            {
                ExternalCustom = externalCustom
            };
            return target;
        }

        Target HomeStart()
        {
            var externalValue = ExternalValue(_loopDistance);

            string declaration = $@"VAR num motorValue:= 0;
PERS num extrusionFactor:={_extrusionFactor: 0.000};
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

            var initCommand = new Robots.Commands.Custom("Init", declaration: declaration, command: initCode)
            {
                RunBefore = true
            };
            var testCommand = new Robots.Commands.Custom("Test", command: testCode);

            var command = new Group(new[] { initCommand, testCommand });
            var home = new JointTarget(_att.Home, _att.Tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame, new[] { totalDistance })
            {
                ExternalCustom = externalCustom
            };
            return home;
        }

        Target HomeEnd()
        {
            var command = new Group()
            {
                new Message("Se acabó."),
                new Stop()
            };
            var home = new JointTarget(_att.Home, _att.Tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame, new[] { totalDistance })
            {
                ExternalCustom = externalCustom
            };
            return home;
        }
    }
}
