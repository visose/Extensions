using Rhino.Geometry;

namespace Extensions.Spatial;

public class Octree<T>(Box box) where T : IPositionable
{
    static readonly int _capacity = 4;
    Box _box = box;
    readonly List<T> _elements = [];
    List<Octree<T>> _children;

    void Subdivide()
    {
        _children =
            [
                new Octree<T>(new Box(_box.Plane, new Interval(_box.X.T0, (_box.X.T0 + _box.X.T1) / 2), new Interval(_box.Y.T0, (_box.Y.T0 + _box.Y.T1) / 2), new Interval(_box.Z.T0, (_box.Z.T0 + _box.Z.T1) / 2))),
                new Octree<T>(new Box(_box.Plane, new Interval(_box.X.T0, (_box.X.T0 + _box.X.T1) / 2), new Interval(_box.Y.T0, (_box.Y.T0 + _box.Y.T1) / 2), new Interval((_box.Z.T0 + _box.Z.T1) / 2, _box.Z.T1))),
                new Octree<T>(new Box(_box.Plane, new Interval((_box.X.T0 + _box.X.T1) / 2, _box.X.T1), new Interval(_box.Y.T0, (_box.Y.T0 + _box.Y.T1) / 2), new Interval((_box.Z.T0 + _box.Z.T1) / 2, _box.Z.T1))),
                new Octree<T>(new Box(_box.Plane, new Interval((_box.X.T0 + _box.X.T1) / 2, _box.X.T1), new Interval(_box.Y.T0, (_box.Y.T0 + _box.Y.T1) / 2), new Interval(_box.Z.T0, (_box.Z.T0 + _box.Z.T1) / 2))),
                new Octree<T>(new Box(_box.Plane, new Interval(_box.X.T0, (_box.X.T0 + _box.X.T1) / 2), new Interval((_box.Y.T0 + _box.Y.T1) / 2, _box.Y.T1), new Interval(_box.Z.T0, (_box.Z.T0 + _box.Z.T1) / 2))),
                new Octree<T>(new Box(_box.Plane, new Interval(_box.X.T0, (_box.X.T0 + _box.X.T1) / 2), new Interval((_box.Y.T0 + _box.Y.T1) / 2, _box.Y.T1), new Interval((_box.Z.T0 + _box.Z.T1) / 2, _box.Z.T1))),
                new Octree<T>(new Box(_box.Plane, new Interval((_box.X.T0 + _box.X.T1) / 2, _box.X.T1), new Interval((_box.Y.T0 + _box.Y.T1) / 2, _box.Y.T1), new Interval((_box.Z.T0 + _box.Z.T1) / 2, _box.Z.T1))),
                new Octree<T>(new Box(_box.Plane, new Interval((_box.X.T0 + _box.X.T1) / 2, _box.X.T1), new Interval((_box.Y.T0 + _box.Y.T1) / 2, _box.Y.T1), new Interval(_box.Z.T0, (_box.Z.T0 + _box.Z.T1) / 2)))
            ];
    }

    public bool Insert(T element)
    {
        if (!_box.Contains(element.Position))
            return false;

        if (_elements.Count < _capacity)
        {
            _elements.Add(element);
            return true;
        }

        if (_children == null)
            Subdivide();

        foreach (var tree in _children)
            if (tree.Insert(element)) return true;

        return false;
    }

    public List<T> SquareClosest(Point3d point, double squareRadius)
    {
        var elementsInRange = new List<T>();

        if (!BoxSphereIntersection(_box, point, squareRadius))
            return elementsInRange;

        foreach (var element in _elements)
        {
            Vector3d vector = point - element.Position;
            if (vector.SquareLength <= squareRadius)
                elementsInRange.Add(element);
        }

        if (_children == null)
            return elementsInRange;

        foreach (var tree in _children)
            elementsInRange.AddRange(tree.SquareClosest(point, squareRadius));

        return elementsInRange;
    }

    public List<T> GetAll()
    {
        var nodeElements = new List<T>();
        nodeElements.AddRange(_elements);
        if (_children != null)
        {
            foreach (var tree in _children)
                nodeElements.AddRange(tree.GetAll());
        }

        return nodeElements;
    }

    bool BoxSphereIntersection(Box box, Point3d center, double squareRadius)
    {
        Point3d corner = new(box.X.T0, box.Y.T0, box.Z.T0);
        double dmin = 0;
        double xLength = box.X.Length;
        double yLength = box.Y.Length;
        double zLength = box.Z.Length;

        if (center.X < corner.X)
            dmin += (center.X - corner.X) * (center.X - corner.X);
        else if (center.X > (corner.X + xLength))
            dmin += (center.X - (corner.X + xLength)) * (center.X - (corner.X + xLength));

        if (center.Y < corner.Y)
            dmin += (center.Y - corner.Y) * (center.Y - corner.Y);
        else if (center.Y > (corner.Y + yLength))
            dmin += (center.Y - (corner.Y + yLength)) * (center.Y - (corner.Y + yLength));

        if (center.Z < corner.Z)
            dmin += (center.Z - corner.Z) * (center.Z - corner.Z);
        else if (center.Z > (corner.Z + zLength))
            dmin += (center.Z - (corner.Z + zLength)) * (center.Z - (corner.Z + zLength));

        return dmin <= squareRadius;
    }
}
