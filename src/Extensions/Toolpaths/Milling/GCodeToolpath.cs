using Rhino.Geometry;
using Robots;
using gs;
using static Extensions.GeometryUtil;

namespace Extensions.Toolpaths.Milling;

public class GCodeToolpath : IToolpath
{
    readonly FiveAxisToRobots _toolpath;

    public IReadOnlyList<Target> Targets => _toolpath.Targets;

    public GCodeToolpath(string file, CartesianTarget referenceTarget, Vector3d alignment, bool addBit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);

        using var reader = File.OpenText(file);

        GenericGCodeParser parser = new();
        var code = parser.Parse(reader);

        _toolpath = new(referenceTarget, alignment, code, addBit);
    }

    public void Deconstruct(out Tool tool, out Frame mcs, out IReadOnlyList<int> rapidStarts, out IReadOnlyList<string> ignored)
    {
        _toolpath.Deconstruct(out tool, out mcs, out rapidStarts, out ignored);
    }
}

class FiveAxisToRobots
{
    readonly List<Target> _targets = [];

    public IReadOnlyList<Target> Targets => _targets;

    readonly List<string> _ignored = [];
    readonly List<int> _rapidStarts = [0];
    Tool _tool;
    readonly Frame _mcs;
    readonly Dictionary<(GCodeLine.LType letter, int number), Action<GCodeLine>> _gCodeMap;
    readonly Dictionary<double, Speed> _speeds = [];
    readonly CartesianTarget _refTarget;
    Vector3d _alignment;
    int _lastRapidLine = 0;

    public void Deconstruct(out Tool tool, out Frame mcs, out IReadOnlyList<int> rapidStarts, out IReadOnlyList<string> ignored)
    {
        tool = _tool;
        mcs = _mcs;
        rapidStarts = _rapidStarts;
        ignored = _ignored;
    }

    public FiveAxisToRobots(CartesianTarget refTarget, Vector3d alignment, GCodeFile file, bool addBit)
    {
        _refTarget = refTarget;
        _alignment = alignment;

        var workPlane = _refTarget.Frame.Plane;
        _mcs = new(plane: workPlane, name: "MCS");

        _gCodeMap = new()
        {
            { (GCodeLine.LType.GCode, 0), RapidMove },
            { (GCodeLine.LType.GCode, 1), LinearMove}
        };

        _tool = _refTarget.Tool;

        if (addBit)
        {
            _gCodeMap.Add((GCodeLine.LType.MCode, 6), ToolSet);
        }

        Interpret(file);
        ArgumentOutOfRangeException.ThrowIfZero(_targets.Count, nameof(file));
        var lastTarget = _targets.Count - 1;
        if (_rapidStarts[^1] != lastTarget)
            _rapidStarts.Add(lastTarget);
    }

    void Interpret(GCodeFile file)
    {
        foreach (GCodeLine line in file.AllLines())
        {
            if (_gCodeMap.TryGetValue((line.type, line.code), out var action))
                action(line);
            else
                Ignore(line);
        }
    }

    void Ignore(GCodeLine line)
    {
        string message = $"{line.lineNumber}: {line.orig_string}";
        _ignored.Add(message);
    }

    bool Move(GCodeLine line)
    {
        var parameters = new[] { "X", "Y", "Z", "A", "B", "F" };
        var v = new double[6];

        for (int i = 0; i < 6; i++)
        {
            if (!GCodeUtil.TryFindParamNum(line.parameters, parameters[i], ref v[i]))
            {
                Ignore(line);
                return false;
            }
        }

        Point3d p = new(v[0], v[1], v[2]);

        var a = v[3].ToRadians();
        var b = v[4].ToRadians();

        var n = PolarToVector(a, b);
        var plane = AlignedPlane(p, n, _alignment);

        Speed speed;

        if (_targets.Count == 0)
        {
            speed = _refTarget.Speed;
        }
        else
        {
            var feed = v[5];

            if (!_speeds.TryGetValue(feed, out Speed? cachedSpeed))
            {
                var referenceSpeed = _refTarget.Speed;
                speed = new(
                    translation: feed / 60.0,
                    rotationSpeed: referenceSpeed.RotationSpeed,
                    translationExternal: referenceSpeed.TranslationExternal,
                    rotationExternal: referenceSpeed.RotationExternal,
                    name: $"Feed{_speeds.Count:000}",
                    translationAccel: referenceSpeed.TranslationAccel,
                    axisAccel: referenceSpeed.AxisAccel,
                    time: referenceSpeed.Time);
                _speeds.Add(feed, speed);
            }
            else
            {
                speed = cachedSpeed;
            }
        }

        CartesianTarget target = new(
            plane,
            null,
            Motions.Linear,
            _tool,
            speed,
            _refTarget.Zone,
            Command.Default,
            _mcs,
            null
            );

        _targets.Add(target);
        return true;
    }

    void RapidMove(GCodeLine line)
    {
        if (!Move(line))
            return;

        int lineNumber = line.lineNumber;
        int targetIndex = _targets.Count - 1;

        if (targetIndex > 0 && lineNumber > 0 && lineNumber - _lastRapidLine > 1)
        {
            _rapidStarts.Add(targetIndex);
        }

        _lastRapidLine = lineNumber;
    }

    void LinearMove(GCodeLine line)
    {
        _ = Move(line);
    }

    void ToolSet(GCodeLine line)
    {
        double length = 60;
        double diameter = 10;
        GCodeUtil.TryFindParamNum(line.parameters, "L", ref length);
        GCodeUtil.TryFindParamNum(line.parameters, "D", ref diameter);

        EndMill endMill = new()
        {
            Length = length,
            Diameter = diameter,
        };

        _tool = endMill.MakeTool(_refTarget.Tool);
    }
}
