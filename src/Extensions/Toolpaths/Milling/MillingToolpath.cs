using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Toolpaths.Milling;

public class MillingToolpath : SimpleToolpath
{
    public List<int> SubPrograms { get; set; } = new List<int>();

    readonly MillingAttributes _att;
    readonly Tool _tool;
    readonly BoundingBox _bbox;

    public MillingToolpath(IList<Polyline> paths, BoundingBox box, MillingAttributes attributes)
    {
        _att = attributes;
        _bbox = box;
        _tool = _att.EndMill.MakeTool(_att.Tool);
        CreateTargets(paths);
    }

    void CreateTargets(IList<Polyline> paths)
    {
        var safeZ = _bbox.Max.Z + _att.SafeZOffset;
        double layerZ = _att.StepDown + _att.SafeZOffset;
        _targets.Add(HomeStart());

        foreach (var path in paths)
        {
            var first = path[0];
            var last = path[path.Count - 1];

            var firstSafe = new Point3d(first.X, first.Y, safeZ);
            _targets.Add(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone));


            var firstOffset = first + Vector3d.ZAxis * layerZ;
            _targets.Add(CreateTarget(firstOffset, _att.SafeSpeed, _att.SafeZone));

            _targets.Add(CreateTarget(first, _att.PlungeSpeed, _att.PlungeZone));

            for (int i = 1; i < path.Count - 1; i++)
                _targets.Add(CreateTarget(path[i], _att.CutSpeed, _att.CutZone));

            _targets.Add(CreateTarget(last, _att.CutSpeed, _att.PlungeZone));

            var lastOffset = last + Vector3d.ZAxis * layerZ;
            _targets.Add(CreateTarget(lastOffset, _att.PlungeSpeed, _att.PlungeZone));

            var lastSafe = new Point3d(last.X, last.Y, safeZ);
            _targets.Add(CreateTarget(lastSafe, _att.SafeSpeed, _att.SafeZone));

            SubPrograms.Add(_targets.Count);
        }

        _targets.Add(HomeEnd());
        SubPrograms.RemoveAt(SubPrograms.Count - 1);
    }

    Target CreateTarget(Point3d position, Speed speed, Zone zone = null)
    {
        var plane = Plane.WorldXY;
        plane.Origin = position;
        var frame = _att.Frame;
        var target = new CartesianTarget(plane, null, Motions.Linear, _tool, speed, zone, null, frame, null);
        return target;
    }

    Target HomeStart()
    {
        var command = new Group()
                {
                    new Message("Press play to start milling..."),
                    new Stop()
                };

        var home = new JointTarget(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
        return home;
    }

    Target HomeEnd()
    {
        var command = new Group()
                {
                    new Message("Se acabó."),
                };

        var home = new JointTarget(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
        return home;
    }
}
