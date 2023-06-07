using Rhino.Geometry;

namespace Extensions.Simulations.DifferentialGrowth;

public struct Force
{
    public Vector3d Vector;
    public double Weight;
    private readonly object _thisLock;

    public Force(Vector3d vector, double weight)
    {
        _thisLock = new();
        Vector = vector;
        Weight = weight;
    }

    public void Add(Vector3d vector, double weight)
    {
        lock (_thisLock)
        {
            Vector += vector;
            Weight += weight;
        }
    }

    public void SetZero()
    {
        Vector = Vector3d.Zero;
        Weight = 0.0;
    }
}
