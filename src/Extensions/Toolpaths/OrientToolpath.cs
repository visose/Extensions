using Rhino.Geometry;
using Robots;
using static Extensions.GeometryUtil;

namespace Extensions.Toolpaths;

public class OrientToolpath
{
    public IToolpath Toolpath { get; }

    public OrientToolpath(IToolpath toolpath, Mesh guide, Vector3d alignment, Mesh? surface = null)
    {
        var targets = new Target[toolpath.Targets.Count];

        for (int i = 0; i < toolpath.Targets.Count; i++)
        {
            var target = toolpath.Targets[i];

            if (target is CartesianTarget cartesian)
            {
                var plane = cartesian.Plane;
                var normal = OrientToMesh(plane.Origin, guide, surface);
                var newPlane = AlignedPlane(plane.Origin, normal, alignment);
                targets[i] = cartesian.WithPlane(newPlane);
            }
            else
            {
                targets[i] = target;
            }
        }

        SimpleToolpath result = new(targets);
        Toolpath = result;
    }
}
