using Robots;

namespace Extensions.Toolpaths.Milling;

public sealed class MillingAttributes
{
    public required EndMill EndMill { get; init; }
    public double StepOver { get; init; }
    public double StepDown { get; init; }

    public double SafeZOffset { get; init; }

    public required Speed SafeSpeed { get; init; }
    public required Speed PlungeSpeed { get; init; }
    public required Speed CutSpeed { get; init; }

    public required Zone SafeZone { get; init; }
    public required Zone PlungeZone { get; init; }
    public required Zone CutZone { get; init; }

    public required Tool Tool { get; init; }
    public required Frame Frame { get; init; }
    public required double[] Home { get; init; }

    public MillingAttributes Initialize()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EndMill.Diameter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EndMill.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(StepDown);
        ArgumentOutOfRangeException.ThrowIfNegative(StepOver);
        ArgumentOutOfRangeException.ThrowIfNotEqual(Home.Length, 6, nameof(Home));

        return new()
        {
            EndMill = EndMill,
            StepOver = StepOver,
            StepDown = StepDown,
            SafeZOffset = SafeZOffset,
            SafeSpeed = SafeSpeed.CloneWithName<Speed>(nameof(SafeSpeed)),
            PlungeSpeed = PlungeSpeed.CloneWithName<Speed>(nameof(PlungeSpeed)),
            CutSpeed = CutSpeed.CloneWithName<Speed>(nameof(CutSpeed)),
            SafeZone = SafeZone.CloneWithName<Zone>(nameof(SafeZone)),
            PlungeZone = PlungeZone.CloneWithName<Zone>(nameof(PlungeZone)),
            CutZone = CutZone.CloneWithName<Zone>(nameof(CutZone)),
            Tool = Tool,
            Frame = Frame.CloneWithName<Frame>(nameof(Frame)),
            Home = [.. Home]
        };
    }
}
