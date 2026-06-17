using Rhino.Geometry;

namespace Extensions.Spatial;

interface IPositionable
{
    int Index { get; }
    ref Point3d Position { get; }
}

struct Vector3i : IEquatable<Vector3i>
{
    public int X;
    public int Y;
    public int Z;

    public Vector3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3i(Point3d point, double scale)
    {
        point *= scale;
        X = point.X >= 0 ? (int)point.X : (int)point.X - 1;
        Y = point.Y >= 0 ? (int)point.Y : (int)point.Y - 1;
        Z = point.Z >= 0 ? (int)point.Z : (int)point.Z - 1;
    }

    public static bool operator ==(Vector3i a, Vector3i b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Vector3i a, Vector3i b) => !(a == b);

    public readonly bool Equals(Vector3i other) => this == other;

    public override readonly bool Equals(object? obj) => obj is Vector3i other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
}

struct Vector2i : IEquatable<Vector2i>
{
    public int X;
    public int Y;

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2i(Point3d point, double scale)
    {
        point *= scale;
        X = point.X >= 0 ? (int)point.X : (int)point.X - 1;
        Y = point.Y >= 0 ? (int)point.Y : (int)point.Y - 1;
    }

    public static bool operator ==(Vector2i a, Vector2i b) => a.X == b.X && a.Y == b.Y;

    public static bool operator !=(Vector2i a, Vector2i b) => !(a == b);

    public readonly bool Equals(Vector2i other) => this == other;

    public override readonly bool Equals(object? obj) => obj is Vector2i other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
}
