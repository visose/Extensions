using Rhino.Geometry;

namespace Extensions.Spatial;

public class BucketSearchSparse2d<T> where T : IPositionable
{
    readonly Dictionary<Vector2i, List<T>> _table;
    readonly double _distanceSquared;
    readonly double _factor;

    public BucketSearchSparse2d(double distance)
    {
        _table = new Dictionary<Vector2i, List<T>>();
        _distanceSquared = distance * distance;
        _factor = 1.0 / distance;
    }

    public void Populate(IEnumerable<T> elements)
    {
        _table.Clear();

        foreach (var element in elements)
        {
            var key = new Vector2i(element.Position, _factor);

            if (!_table.TryGetValue(key, out List<T> bucket))
            {
                bucket = new List<T>();
                _table.Add(key, bucket);
            }

            bucket.Add(element);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        var center = new Vector2i(element.Position, _factor);
        var keys = new Vector2i[9];

        int count = 0;
        for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                keys[count++] = new Vector2i(center.X + i, center.Y + j);

        var elements = new List<T>();

        foreach (var key in keys)
        {
            if (!_table.TryGetValue(key, out List<T> value)) continue;

            foreach (var other in value)
            {
                if (element.Equals(other)) continue;

                double distance = (element.Position - other.Position).SquareLength;
                if (distance <= _distanceSquared)
                    elements.Add(other);
            }
        }

        return elements;
    }
}

public class BucketSearchSparse3d<T> where T : IPositionable
{
    readonly Dictionary<Vector3i, List<T>> _table;
    readonly double _distanceSquared;
    readonly double _factor;

    public BucketSearchSparse3d(double distance)
    {
        _table = new Dictionary<Vector3i, List<T>>();
        _distanceSquared = distance * distance;
        _factor = 1.0 / distance;
    }

    public void Populate(IEnumerable<T> elements)
    {
        _table.Clear();

        foreach (var element in elements)
        {
            var key = new Vector3i(element.Position, _factor);

            if (!_table.TryGetValue(key, out List<T> bucket))
            {
                bucket = new List<T>();
                _table.Add(key, bucket);
            }

            bucket.Add(element);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        var position = element.Position;
        var index = element.Index;

        var center = new Vector3i(element.Position, _factor);
        var keys = new Vector3i[27];

        int count = 0;
        for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                for (int k = -1; k < 2; k++)
                    keys[count++] = new Vector3i(center.X + i, center.Y + j, center.Z + k);

        var elements = new List<T>();

        foreach (var key in keys)
        {
            if (!_table.TryGetValue(key, out List<T> value)) continue;

            foreach (var other in value)
            {
                if (other.Index >= index) continue;

                double distance = (position - other.Position).SquareLength;
                if (distance <= _distanceSquared)
                    elements.Add(other);
            }
        }

        return elements;
    }
}


public class BucketSearchDense2d<T> where T : IPositionable
{
    readonly List<T>[][] _table;
    readonly double _distanceSquared;
    readonly double _factor;
    Vector2i _start;
    Vector2i _size;

    public BucketSearchDense2d(BoundingBox box, double distance)
    {
        _distanceSquared = distance * distance;
        _factor = 1.0 / distance;

        _start = new Vector2i(box.Corner(true, true, true), _factor);
        var end = new Vector2i(box.Corner(false, false, false), _factor);
        _size = new Vector2i(end.X - _start.X + 1, end.Y - _start.Y + 1);

        _table = new List<T>[_size.X][];
        for (int i = 0; i < _size.X; i++)
            _table[i] = new List<T>[_size.Y];
    }

    public void Populate(IEnumerable<T> elements)
    {
        for (int i = 0; i < _size.X; i++)
            for (int j = 0; j < _size.Y; j++)
            {
                var bucket = _table[i][j];
                bucket?.Clear();
            }

        foreach (var element in elements)
        {
            var key = new Vector2i(element.Position, _factor);
            key.X -= _start.X;
            key.Y -= _start.Y;

            if (key.X < 0 || key.Y < 0) continue;
            if (key.X >= _size.X || key.Y >= _size.Y) continue;

            List<T> bucket = _table[key.X][key.Y];

            if (bucket == null)
            {
                bucket = new List<T>();
                _table[key.X][key.Y] = bucket;
            }

            bucket.Add(element);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        var center = new Vector2i(element.Position, _factor);
        center.X -= _start.X;
        center.Y -= _start.Y;
        var keys = new Vector2i[9];

        int count = 0;
        for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                keys[count++] = new Vector2i(center.X + i, center.Y + j);

        var elements = new List<T>();

        foreach (var key in keys)
        {
            if (key.X < 0 || key.Y < 0) continue;
            if (key.X >= _size.X || key.Y >= _size.Y) continue;

            List<T> bucket = _table[key.X][key.Y];
            if (bucket == null) continue;

            foreach (var other in bucket)
            {
                if (element.Equals(other)) continue;

                double distance = (element.Position - other.Position).SquareLength;
                if (distance <= _distanceSquared)
                    elements.Add(other);
            }
        }

        return elements;
    }
}

public class BucketSearchDense3d<T> where T : IPositionable
{
    readonly List<int>[,,] _table;
    Element<T>[] _elements;
    readonly double _distanceSquared;
    readonly double _factor;
    Vector3i _start;
    Vector3i _size;

    public BucketSearchDense3d(BoundingBox box, double distance)
    {
        _distanceSquared = distance * distance;
        _factor = 1.0 / distance;

        _start = new Vector3i(box.Corner(true, true, true), _factor);
        var end = new Vector3i(box.Corner(false, false, false), _factor);
        _size = new Vector3i(end.X - _start.X + 1, end.Y - _start.Y + 1, end.Z - _start.Z + 1);
        _table = new List<int>[_size.X, _size.Y, _size.Z];
    }

    public void Populate(IList<T> elements)
    {
        _elements = new Element<T>[elements.Count];

        for (int i = 0; i < elements.Count; i++)
        {
            var e = elements[i];
            _elements[i] = new Element<T> { Index = e.Index, Position = e.Position, Value = e };
        }

        for (int i = 0; i < _size.X; i++)
            for (int j = 0; j < _size.Y; j++)
                for (int k = 0; k < _size.Z; k++)
                {
                    var bucket = _table[i, j, k];
                    bucket?.Clear();
                }

        foreach (var element in elements)
        {
            var key = new Vector3i(element.Position, _factor);
            key.X -= _start.X;
            key.Y -= _start.Y;
            key.Z -= _start.Z;

            if (key.X < 0 || key.Y < 0 || key.Z < 0) continue;
            if (key.X >= _size.X || key.Y >= _size.Y || key.Z >= _size.Z) continue;

            List<int> bucket = _table[key.X, key.Y, key.Z];

            if (bucket == null)
            {
                bucket = new List<int>();
                _table[key.X, key.Y, key.Z] = bucket;
            }

            bucket.Add(element.Index);
        }
    }

    public IEnumerable<T> GetClosests(T element)
    {
        int index = element.Index;
        Point3d a = element.Position;

        var center = new Vector3i(a, _factor);
        center.X -= _start.X;
        center.Y -= _start.Y;
        center.Z -= _start.Z;

        var keys = new Vector3i[27];

        int count = 0;
        for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                for (int k = -1; k < 2; k++)
                    keys[count++] = new Vector3i(center.X + i, center.Y + j, center.Z + k);

        int itemCount = 0;

        foreach (var key in keys)
        {
            if (key.X < 0 || key.Y < 0 || key.Z < 0) continue;
            if (key.X >= _size.X || key.Y >= _size.Y || key.Z >= _size.Z) continue;
            var bucket = _table[key.X, key.Y, key.Z];
            if (bucket != null)
                itemCount += bucket.Count;
        }

        var elements = new List<T>(itemCount);

        foreach (var key in keys)
        {
            if (key.X < 0 || key.Y < 0 || key.Z < 0) continue;
            if (key.X >= _size.X || key.Y >= _size.Y || key.Z >= _size.Z) continue;

            List<int> bucket = _table[key.X, key.Y, key.Z];
            if (bucket == null) continue;

            foreach (var otherIndex in bucket)
            {
                if (otherIndex >= index) continue;
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

    struct Element<Q>
    {
        public int Index;
        public Point3d Position;
        public Q Value;
    }
}
