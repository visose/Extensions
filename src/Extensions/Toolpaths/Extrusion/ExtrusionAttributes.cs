using Robots;

namespace Extensions.Toolpaths.Extrusion;

public sealed class ExtrusionAttributes
{
    public double NozzleDiameter { get; init; }
    public double LayerHeight { get; init; }
    public double BeadWidth { get; init; }

    public double SafeZOffset { get; init; }

    public required Speed SafeSpeed { get; init; }
    public required Speed ApproachSpeed { get; init; }
    public required Speed ExtrusionSpeed { get; init; }

    public required Zone SafeZone { get; init; }
    public required Zone ApproachZone { get; init; }
    public required Zone ExtrusionZone { get; init; }

    public required Tool Tool { get; init; }
    public required Frame Frame { get; init; }
    public required double[] Home { get; init; }

    public ExtrusionAttributes Initialize()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(NozzleDiameter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(LayerHeight);
        ArgumentOutOfRangeException.ThrowIfNotEqual(Home.Length, 6, nameof(Home));

        return new()
        {
            NozzleDiameter = NozzleDiameter,
            LayerHeight = LayerHeight,
            BeadWidth = Util.GetWidth(NozzleDiameter, LayerHeight),
            SafeZOffset = SafeZOffset,
            SafeSpeed = SafeSpeed.CloneWithName<Speed>(nameof(SafeSpeed)),
            ApproachSpeed = ApproachSpeed.CloneWithName<Speed>(nameof(ApproachSpeed)),
            ExtrusionSpeed = ExtrusionSpeed.CloneWithName<Speed>(nameof(ExtrusionSpeed)),
            SafeZone = SafeZone.CloneWithName<Zone>(nameof(SafeZone)),
            ApproachZone = ApproachZone.CloneWithName<Zone>(nameof(ApproachZone)),
            ExtrusionZone = ExtrusionZone.CloneWithName<Zone>(nameof(ExtrusionZone)),
            Tool = Tool,
            Frame = Frame.CloneWithName<Frame>(nameof(Frame)),
            Home = [.. Home]
        };
    }
}
