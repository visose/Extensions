using Rhino.Geometry;

namespace Extensions.Spatial;

class BucketSearchSparse2d<T>(double distance) where T : IPositionable
{
    readonly Dictionary<Vector2i, List<T>> _table = [];
    readonly double _distanceSquared = distance * distance;
    readonly double _factor = 1.0 / distance;

    public void Populate(IEnumerable<T> elements)
    {
        _table.Clear();

        foreach (var element in elements)
        {
            Vector2i key = new(element.Position, _factor);

            if (!_table.TryGetValue(key, out List<T>? bucket))
            {
                bucket = [];
                _table.Add(key, bucket);
            }

            bucket.Add(element);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        Vector2i center = new(element.Position, _factor);
        var keys = new Vector2i[9];

        int count = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
                keys[count++] = new(center.X + i, center.Y + j);
        }

        List<T> elements = [];

        foreach (var key in keys)
        {
            if (!_table.TryGetValue(key, out List<T>? value))
                continue;

            foreach (var other in value)
            {
                if (element.Equals(other))
                    continue;

                double distance = (element.Position - other.Position).SquareLength;
                if (distance <= _distanceSquared)
                    elements.Add(other);
            }
        }

        return elements;
    }
}

class BucketSearchDense3d<T> where T : IPositionable
{
    readonly List<int>?[,,] _table;
    Element<T>[] _elements = [];
    readonly double _distanceSquared;
    readonly double _factor;
    Vector3i _start;
    Vector3i _size;

    public BucketSearchDense3d(BoundingBox box, double distance)
    {
        _distanceSquared = distance * distance;
        _factor = 1.0 / distance;

        _start = new(box.Corner(true, true, true), _factor);
        Vector3i end = new(box.Corner(false, false, false), _factor);
        _size = new(end.X - _start.X + 1, end.Y - _start.Y + 1, end.Z - _start.Z + 1);
        _table = new List<int>?[_size.X, _size.Y, _size.Z];
    }

    public void Populate(IList<T> elements)
    {
        _elements = new Element<T>[elements.Count];

        for (int i = 0; i < elements.Count; i++)
        {
            var e = elements[i];
            _elements[i] = new() { Index = e.Index, Position = e.Position, Value = e };
        }

        for (int i = 0; i < _size.X; i++)
        {
            for (int j = 0; j < _size.Y; j++)
            {
                for (int k = 0; k < _size.Z; k++)
                {
                    var bucket = _table[i, j, k];
                    bucket?.Clear();
                }
            }
        }

        foreach (var element in elements)
        {
            Vector3i key = new(element.Position, _factor);
            key.X -= _start.X;
            key.Y -= _start.Y;
            key.Z -= _start.Z;

            if (IsOutside(key))
                continue;

            List<int>? bucket = _table[key.X, key.Y, key.Z];

            if (bucket == null)
            {
                bucket = [];
                _table[key.X, key.Y, key.Z] = bucket;
            }

            bucket.Add(element.Index);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        int index = element.Index;
        Point3d a = element.Position;

        Vector3i center = new(a, _factor);
        center.X -= _start.X;
        center.Y -= _start.Y;
        center.Z -= _start.Z;

        var keys = new Vector3i[27];

        int count = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                for (int k = -1; k < 2; k++)
                    keys[count++] = new(center.X + i, center.Y + j, center.Z + k);
            }
        }

        int itemCount = 0;

        foreach (var key in keys)
        {
            if (IsOutside(key))
                continue;

            var bucket = _table[key.X, key.Y, key.Z];
            if (bucket != null)
                itemCount += bucket.Count;
        }

        List<T> elements = new(itemCount);

        foreach (var key in keys)
        {
            if (IsOutside(key))
                continue;

            List<int>? bucket = _table[key.X, key.Y, key.Z];

            if (bucket == null)
                continue;

            foreach (var otherIndex in bucket)
            {
                if (otherIndex >= index)
                    continue;
                var other = _elements[otherIndex];

                Point3d b = other.Position;
                double dx = a.X - b.X;
                double dy = a.Y - b.Y;
                double dz = a.Z - b.Z;

                double distance = dx * dx + dy * dy + dz * dz;

                if (distance <= _distanceSquared)
                    elements.Add(other.Value);
            }
        }

        return elements;
    }

    bool IsOutside(Vector3i key) =>
        key.X < 0 || key.Y < 0 || key.Z < 0 ||
        key.X >= _size.X || key.Y >= _size.Y || key.Z >= _size.Z;

    struct Element<Q>
    {
        public int Index;
        public Point3d Position;
        public Q Value;
    }
}
