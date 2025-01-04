using Rhino.Geometry;

namespace Extensions.Simulations.DifferentialGrowth;

public struct Force(Vector3d vector, double weight)
{
    public Vector3d Vector = vector;
    public double Weight = weight;
    private readonly object _thisLock = new();

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
