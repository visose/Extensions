using Rhino.Geometry;
using Robots;
using System.Globalization;

namespace Extensions.Toolpaths;

public class CSVConverter
{
    readonly List<Target> _targets = [];
    readonly List<Polyline> _toolPath = [];

    public IReadOnlyList<Target> Targets => _targets;
    public IReadOnlyList<Polyline> ToolPath => _toolPath;

    static readonly string[] _validParameters = ["type", "position", "normal", "xaxis", "speed", "zone"];

    public CSVConverter(string file, CartesianTarget referenceTarget, string mask, bool reverse, double cutSpeed = 0, Point3d? point = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(mask);

        var splitMask = mask.Split(',').Select(p => p.Trim().ToLowerInvariant()).ToArray();

        if (!splitMask.All(p => _validParameters.Contains(p)))
            throw new ArgumentException("Mask is not valid.", nameof(mask));

        var parameterIndex = splitMask.ToDictionary(p => p, p => -1);

        int count = 0;

        foreach (var parameter in splitMask)
        {
            parameterIndex[parameter] = count;
            if (parameter == "position" || parameter == "normal" || parameter == "xaxis")
                count += 3;
            else
                count += 1;
        }

        var lines = File.ReadAllLines(file);

        List<Plane> planes = new(lines.Length);
        List<double> speedValues = new(lines.Length);
        List<double> zoneValues = new(lines.Length);
        List<double> types = new(lines.Length);

        foreach (var line in lines)
        {
            var fields = line.Split(',');

            if (fields.Length != count)
                throw new FormatException($"CSV line has {fields.Length} values, expected {count}.");

            var numbers = fields.Select(s =>
            {
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double n))
                    throw new FormatException($"Cannot convert field '{s}' to a number.");

                return n;
            }).ToArray();

            Vector3d GetVector(int index) => new(numbers[index], numbers[index + 1], numbers[index + 2]);

            var position = parameterIndex.TryGetValue("position", out int positionIndex) ? (Point3d)GetVector(positionIndex) : referenceTarget.Plane.Origin;
            var normal = parameterIndex.TryGetValue("normal", out int normalIndex) ? GetVector(normalIndex) : referenceTarget.Plane.Normal;
            if (reverse)
                normal *= -1.0;
            var xaxis = parameterIndex.TryGetValue("xaxis", out int xaxisIndex) ? GetVector(xaxisIndex) : referenceTarget.Plane.XAxis;

            if (point != null)
            {
                var localPoint = (Point3d)point;
                localPoint.Transform(Transform.PlaneToPlane(referenceTarget.Frame.Plane, Plane.WorldXY));
                xaxis = localPoint - position;
            }

            Plane plane = new(position, normal);
            double angle = Vector3d.VectorAngle(plane.XAxis, xaxis, plane);
            plane.Rotate(angle, plane.Normal);
            planes.Add(plane);

            double speed = 0;

            if (parameterIndex.TryGetValue("speed", out int speedIndex))
            {
                speed = numbers[speedIndex];
                speedValues.Add(speed * (1.0 / 60.0));
            }

            if (parameterIndex.TryGetValue("zone", out int zoneIndex))
                zoneValues.Add(numbers[zoneIndex]);

            if (parameterIndex.TryGetValue("type", out int typeIndex))
                types.Add(numbers[typeIndex]);
            else
                types.Add(speed <= cutSpeed ? 1 : 0);
        }

        var speedsByValue = speedValues
            .Distinct()
            .ToDictionary(
                static s => s,
                s => new Speed(translation: s, rotationSpeed: referenceTarget.Speed.RotationSpeed));
        var speeds = speedValues.Select(v => speedsByValue[v]).ToArray();

        var zonesByValue = zoneValues
            .Distinct()
            .ToDictionary(static z => z, static z => new Zone(distance: z));
        var zones = zoneValues.Select(v => zonesByValue[v]).ToArray();

        for (int i = 0; i < planes.Count; i++)
        {
            var speed = parameterIndex.ContainsKey("speed") ? speeds[i] : referenceTarget.Speed;
            var zone = parameterIndex.ContainsKey("zone") ? zones[i] : referenceTarget.Zone;

            CartesianTarget target = new(planes[i], null, Motions.Joint, referenceTarget.Tool, speed, zone, null, referenceTarget.Frame);
            _targets.Add(target);
        }

        {
            Polyline? polyline = null;

            for (int i = 0; i < planes.Count; i++)
            {
                bool cutting = types[i] == 1;
                Point3d vertex = planes[i].Origin;

                if (cutting)
                {
                    if (polyline == null)
                    {
                        polyline = [];
                        _toolPath.Add(polyline);
                    }

                    polyline.Add(vertex);
                }
                else
                {
                    polyline = null;
                }
            }
        }

        foreach (var polyline in _toolPath)
        {
            polyline.CollapseShortSegments(0.01);
        }
    }
}
