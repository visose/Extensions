using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Toolpaths.SpatialExtrusion;

public class SpatialAttributes
{
    readonly List<GeometryBase> _environment;

    public double Diameter { get; }
    public double VerticalOffset { get; }
    public double RotationOffset { get; }
    public double DistancePlunge { get; }
    public double DistanceAhead { get; }
    public double DistanceHorizontal { get; }

    public IReadOnlyList<GeometryBase> Environment => _environment;

    public CartesianTarget ReferenceTarget { get; }

    public Speed Approach { get; }
    public Speed Plunge { get; }
    public Speed Fast { get; }
    public Speed Medium { get; }
    public Speed Slow { get; }

    public Command StopExtrusion { get; }
    public Command FastExtrusion { get; }
    public Command MediumExtrusion { get; }
    public Command SlowExtrusion { get; }

    public Command LongWait { get; }
    public Command ShortWait { get; }

    public Command AheadCommand { get; }

    public SpatialAttributes(IReadOnlyList<double> variables, CartesianTarget target, IReadOnlyList<double> speeds, IReadOnlyList<double> waits, IReadOnlyList<int> dos, IReadOnlyList<GeometryBase> environment)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(variables.Count, 6, nameof(variables));
        ArgumentOutOfRangeException.ThrowIfNotEqual(speeds.Count, 5, nameof(speeds));
        ArgumentOutOfRangeException.ThrowIfNotEqual(waits.Count, 4, nameof(waits));
        ArgumentOutOfRangeException.ThrowIfNotEqual(dos.Count, 2, nameof(dos));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(variables[0], nameof(variables));
        ArgumentOutOfRangeException.ThrowIfNegative(variables[1], nameof(variables));
        ArgumentOutOfRangeException.ThrowIfNegative(variables[2], nameof(variables));
        ArgumentOutOfRangeException.ThrowIfNegative(variables[4], nameof(variables));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(variables[4], 1, nameof(variables));
        ArgumentOutOfRangeException.ThrowIfNegative(variables[5], nameof(variables));

        foreach (var speed in speeds)
            ArgumentOutOfRangeException.ThrowIfNegative(speed, nameof(speeds));

        foreach (var wait in waits)
            ArgumentOutOfRangeException.ThrowIfNegative(wait, nameof(waits));

        foreach (var index in dos)
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(dos));

        Diameter = variables[0];
        DistancePlunge = variables[1];
        VerticalOffset = variables[2];
        RotationOffset = variables[3];
        DistanceAhead = variables[4];
        DistanceHorizontal = variables[5];

        _environment = [.. environment];

        ReferenceTarget = target;

        Approach = new(name: "Approach", translation: speeds[0]);
        Plunge = new(name: "Plunge", translation: speeds[1]);
        Fast = new(name: "FastExtrusion", translation: speeds[2]);
        Medium = new(name: "MediumExtrusion", translation: speeds[3]);
        Slow = new(name: "SlowExtrusion", translation: speeds[4]);

        Wait waitAfterStart = new(waits[0]);

        Group stopExtrusion = new([new SetDO(dos[0], false), new SetDO(dos[1], false)]);
        Group fastExtrusion = new([new SetDO(dos[0], true), new SetDO(dos[1], false), waitAfterStart]);
        Group mediumExtrusion = new([new SetDO(dos[0], false), new SetDO(dos[1], true), waitAfterStart]);
        Group slowExtrusion = new([new SetDO(dos[0], true), new SetDO(dos[1], true), waitAfterStart]);

        StopExtrusion = stopExtrusion;
        FastExtrusion = fastExtrusion;
        MediumExtrusion = mediumExtrusion;
        SlowExtrusion = slowExtrusion;

        Group longWait = new([StopExtrusion, new Wait(waits[3])]);
        Group shortWait = new([StopExtrusion, new Wait(waits[2])]);
        Group aheadCommand = new([StopExtrusion, new Wait(waits[1])]);

        LongWait = longWait;
        ShortWait = shortWait;
        AheadCommand = aheadCommand;
    }
}
