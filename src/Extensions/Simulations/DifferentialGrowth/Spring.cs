using Rhino.Geometry;

namespace Extensions.Simulations.DifferentialGrowth;

class Spring
{
    public Particle Start;
    public Particle End;
    double _restLength;
    public double Length;
    public Vector3d Vector;
    readonly DifferentialGrowth _simulation;

    public double RestLength
    {
        get => _restLength;
        set => _restLength = Math.Max(0, value);
    }

    public void Update()
    {
        Vector = new(End.Position - Start.Position);
        Length = Vector.Length;
    }

    public Spring(Particle start, Particle end, int i, DifferentialGrowth simulation)
    {
        Start = start;
        End = end;
        _simulation = simulation;

        start.Neighbours[1] = end;
        end.Neighbours[0] = start;
        Update();
        ArgumentOutOfRangeException.ThrowIfZero(Length, nameof(Length));

        _restLength = Length;
        simulation.Springs.Insert(i, this);
    }

    public void Forces(double weight)
    {
        Vector3d vector = Vector;
        vector *= ((Length - _restLength) / Length) * 0.5;
        Start.Delta.Add(vector * weight, weight);
        End.Delta.Add(-vector * weight, weight);
    }

    public void Split(int i)
    {
        _simulation.Springs.Remove(this);
        Particle mid = new((Start.Position + End.Position) * 0.5, _simulation);
        new Spring(mid, End, i, _simulation);
        new Spring(Start, mid, i, _simulation);
    }
}
