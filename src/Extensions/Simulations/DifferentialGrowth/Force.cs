using Rhino.Geometry;

namespace Extensions.Simulations.DifferentialGrowth;

public struct Force
{
    public Vector3d Vector;
    public double Weight;
    private readonly Object thisLock;

    public Force(Vector3d vector, double weight)
    {
        thisLock = new Object();
        Vector = vector;
        Weight = weight;
    }

    public void Add(Vector3d vector, double weight)
    {
        lock (thisLock)
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
