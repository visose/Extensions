using Rhino.Geometry;
using Robots;
using gs;
using static Extensions.GeometryUtil;

namespace Extensions.Toolpaths.Milling;

public class GCodeToolpath : SimpleToolpath
{
    public FiveAxisToRobots Toolpath { get; set; }

    public GCodeToolpath(string file, CartesianTarget referenceTarget, Vector3d alignment, bool addBit)
    {
        using var reader = File.OpenText(file);

        var parser = new GenericGCodeParser();
        var code = parser.Parse(reader);

        Toolpath = new FiveAxisToRobots(referenceTarget, alignment, code, addBit);
        _targets = Toolpath.Targets;
    }
}

public class FiveAxisToRobots
{
    public List<Target> Targets { get; set; } = [];

    readonly List<string> _ignored = [];
    readonly List<int> _rapidStarts = [0];
    Tool _tool;
    readonly Frame _mcs;
    readonly Dictionary<(GCodeLine.LType letter, int number), Action<GCodeLine>> _gCodeMap;
    readonly Dictionary<double, Speed> _speeds = [];
    readonly CartesianTarget _refTarget;
    Vector3d _alignment;
    int _lastRapid = 0;

    public void Deconstruct(out Tool tool, out Frame mcs, out List<int> rapidStarts, out List<string> ignored)
    {
        tool = _tool;
        mcs = _mcs;
        rapidStarts = _rapidStarts;
        ignored = _ignored;
    }

    internal FiveAxisToRobots(CartesianTarget refTarget, Vector3d alignment, GCodeFile file, bool addBit)
    {
        _refTarget = refTarget;
        _alignment = alignment;

        var workPlane = _refTarget.Frame.Plane;
        //var xform = Transform.PlaneToPlane(Plane.WorldXY, workPlane);
        //var constructionPlane = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetConstructionPlane().Plane;
        //constructionPlane.Origin = Point3d.Origin;
        //constructionPlane.Transform(xform);

        _mcs = new Frame(plane: workPlane, name: "MCS");

        _gCodeMap = new Dictionary<(GCodeLine.LType letter, int number), Action<GCodeLine>>
        {
            { (GCodeLine.LType.GCode, 0), RapidMove },
            { (GCodeLine.LType.GCode, 1), LinearMove}
        };

        if (addBit)
        {
            _gCodeMap.Add((GCodeLine.LType.MCode, 6), ToolSet);
        }
        else
        {
            _tool = _refTarget.Tool;
        }

        Interpret(file);
        _rapidStarts.Add(Targets.Count - 1);
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

    void Move(GCodeLine line)
    {
        var parameters = new[] { "X", "Y", "Z", "A", "B", "F" };
        var v = new double[6];

        for (int i = 0; i < 6; i++)
        {
            if (!GCodeUtil.TryFindParamNum(line.parameters, parameters[i], ref v[i]))
            {
                Ignore(line);
                return;
            }
        }

        var p = new Point3d(v[0], v[1], v[2]);

        var a = v[3].ToRadians();
        var b = v[4].ToRadians();

        var n = PolarToVector(a, b);
        var plane = AlignedPlane(p, n, _alignment);

        Speed speed;

        if (Targets.Count == 0)
        {
            speed = _refTarget.Speed;
        }
        else
        {
            var feed = v[5];

            if (!_speeds.TryGetValue(feed, out speed))
            {
                speed = _refTarget.Speed.CloneWithName<Speed>($"Feed{_speeds.Count:000}");
                speed.TranslationSpeed = feed / 60.0;
                _speeds.Add(feed, speed);
            }
        }

        var target = new CartesianTarget(
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

        Targets.Add(target);
    }

    void RapidMove(GCodeLine line)
    {
        Move(line);

        int i = line.lineNumber;
        if ((i > 0) && (i - _lastRapid > 1))
        {
            _rapidStarts.Add(i);
        }

        _lastRapid = i;
    }

    void LinearMove(GCodeLine line)
    {
        Move(line);
    }

    void ToolSet(GCodeLine line)
    {
        double length = 60;
        double diameter = 10;
        GCodeUtil.TryFindParamNum(line.parameters, "L", ref length);
        GCodeUtil.TryFindParamNum(line.parameters, "D", ref diameter);

        var endMill = new EndMill()
        {
            Length = length,
            Diameter = diameter,
        };

        _tool = endMill.MakeTool(_refTarget.Tool);
    }
}
