using Rhino.Geometry;

namespace Extensions.Simulations.DifferentialGrowth;

public class Spring
{
    public Particle Start;
    public Particle End;
    double _restLength;
    public double Length;
    public Vector3d Vector;
    readonly DifferentialGrowth _simulation;

    public Line Line { get { return new Line(Start.Position, End.Position); } }
    public Point3d Mid { get { return new Point3d((Start.Position + End.Position) / 2); } }

    public double RestLength
    {
        get { return _restLength; }
        set
        {
            _restLength = Math.Max(0, value);
        }
    }

    public void Update()
    {
        Vector = new Vector3d(End.Position - Start.Position);
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
        //  Start.Neighbours.Remove(End);
        //  End.Neighbours.Remove(Start);
        _simulation.Springs.Remove(this);
        var mid = new Particle((Start.Position + End.Position) * 0.5, _simulation);
        new Spring(mid, End, i, _simulation);
        new Spring(Start, mid, i, _simulation);
    }
}
