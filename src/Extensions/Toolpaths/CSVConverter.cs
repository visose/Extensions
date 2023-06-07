using Rhino.Geometry;
using Robots;

namespace Extensions.Toolpaths;

// pX,pY,pZ,nX,nY,nZ,F'

public class CSVConverter
{
    public List<Target> Targets { get; } = new List<Target>();
    public List<Polyline> ToolPath { get; } = new List<Polyline>();

    readonly string[] _validParameters = { "type", "position", "normal", "xaxis", "speed", "zone" };

    public CSVConverter(string file, CartesianTarget referenceTarget, string mask, bool reverse, double cutSpeed = 0, Point3d? point = null)
    {
        var splitMask = mask.Split(',').Select(p => p.Trim().ToLower());
        if (!splitMask.All(p => _validParameters.Contains(p))) throw new Exception(" Mask not valid.");

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

        var planes = new List<Plane>(lines.Length);
        var speedValues = new List<double>(lines.Length);
        var zoneValues = new List<double>(lines.Length);
        var types = new List<double>(lines.Length);

        foreach (var line in lines)
        {
            var fields = line.Split(',');
            if (fields.Length != count) continue; //throw new Exception(" Number of values in a line is not correct.");

            var numbers = fields.Select(s =>
            {
                if (!double.TryParse(s, out double n)) throw new Exception(" Can't convert field to number.");
                return n;
            }).ToArray();

            Vector3d GetVector(int index) => new(numbers[index], numbers[index + 1], numbers[index + 2]);

            var position = parameterIndex.TryGetValue("position", out int positionIndex) ? (Point3d)GetVector(positionIndex) : referenceTarget.Plane.Origin;
            var normal = parameterIndex.TryGetValue("normal", out int normalIndex) ? GetVector(normalIndex) : referenceTarget.Plane.Normal;
            if (reverse) normal *= -1.0;
            var xaxis = parameterIndex.TryGetValue("xaxis", out int xaxisIndex) ? GetVector(xaxisIndex) : referenceTarget.Plane.XAxis;

            if (point != null)
            {
                var localPoint = (Point3d)point;
                localPoint.Transform(Transform.PlaneToPlane(referenceTarget.Frame.Plane, Plane.WorldXY));
                xaxis = localPoint - position;
            }

            var plane = new Plane(position, normal);
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

        var distinctSpeeds = speedValues.Distinct().Select(s => new Speed(translation: s, rotationSpeed: referenceTarget.Speed.RotationSpeed));
        var speeds = speedValues.Select(v => distinctSpeeds.First(s => s.TranslationSpeed == v)).ToList();

        var distinctZones = zoneValues.Distinct().Select(z => new Zone(distance: z));
        var zones = zoneValues.Select(v => distinctZones.First(z => z.Distance == v)).ToList();

        for (int i = 0; i < planes.Count; i++)
        {
            var speed = parameterIndex.ContainsKey("speed") ? speeds[i] : referenceTarget.Speed;
            var zone = parameterIndex.ContainsKey("zone") ? zones[i] : referenceTarget.Zone;

            var target = new CartesianTarget(planes[i], null, Motions.Joint, referenceTarget.Tool, speed, zone, null, referenceTarget.Frame);
            Targets.Add(target);
        }

        {
            Polyline polyline = null;

            for (int i = 0; i < planes.Count; i++)
            {
                bool cutting = types[i] == 1;
                Point3d vertex = planes[i].Origin;

                if (cutting)
                {
                    if (polyline == null)
                    {
                        polyline = new Polyline();
                        ToolPath.Add(polyline);
                    }

                    polyline.Add(vertex);
                }
                else
                {
                    polyline = null;
                }
            }
        }

        foreach (var polyline in ToolPath)
        {
            polyline.CollapseShortSegments(0.01);
        }
    }
}
