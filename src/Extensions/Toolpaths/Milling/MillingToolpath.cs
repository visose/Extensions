using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Toolpaths.Milling;

public class MillingToolpath : TargetListToolpath
{
    readonly List<int> _subPrograms = [];

    public IReadOnlyList<int> SubPrograms => _subPrograms;

    readonly MillingAttributes _att;
    readonly Tool _tool;
    readonly BoundingBox _bbox;

    public MillingToolpath(IReadOnlyList<Polyline> paths, BoundingBox box, MillingAttributes attributes)
    {
        ArgumentOutOfRangeException.ThrowIfZero(paths.Count, nameof(paths));

        _att = attributes;
        _bbox = box;
        _tool = _att.EndMill.MakeTool(_att.Tool);
        CreateTargets(paths);
    }

    void CreateTargets(IReadOnlyList<Polyline> paths)
    {
        var safeZ = _bbox.Max.Z + _att.SafeZOffset;
        double layerZ = _att.StepDown + _att.SafeZOffset;
        AddTarget(HomeStart());

        foreach (var path in paths)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(path.Count, 2, nameof(paths));

            var first = path[0];
            var last = path[path.Count - 1];

            Point3d firstSafe = new(first.X, first.Y, safeZ);
            AddTarget(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone));

            var firstOffset = first + Vector3d.ZAxis * layerZ;
            AddTarget(CreateTarget(firstOffset, _att.SafeSpeed, _att.SafeZone));

            AddTarget(CreateTarget(first, _att.PlungeSpeed, _att.PlungeZone));

            for (int i = 1; i < path.Count - 1; i++)
                AddTarget(CreateTarget(path[i], _att.CutSpeed, _att.CutZone));

            AddTarget(CreateTarget(last, _att.CutSpeed, _att.PlungeZone));

            var lastOffset = last + Vector3d.ZAxis * layerZ;
            AddTarget(CreateTarget(lastOffset, _att.PlungeSpeed, _att.PlungeZone));

            Point3d lastSafe = new(last.X, last.Y, safeZ);
            AddTarget(CreateTarget(lastSafe, _att.SafeSpeed, _att.SafeZone));

            _subPrograms.Add(TargetCount);
        }

        AddTarget(HomeEnd());
        _subPrograms.RemoveAt(_subPrograms.Count - 1);
    }

    Target CreateTarget(Point3d position, Speed speed, Zone? zone = null)
    {
        var plane = Plane.WorldXY;
        plane.Origin = position;
        var frame = _att.Frame;
        CartesianTarget target = new(plane, null, Motions.Linear, _tool, speed, zone, null, frame, null);
        return target;
    }

    Target HomeStart()
    {
        Group command = new([new Message("Press play to start milling..."), new Stop()]);

        JointTarget home = new(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
        return home;
    }

    Target HomeEnd()
    {
        Group command = new([new Message("Se acabó.")]);

        JointTarget home = new(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
        return home;
    }
}
