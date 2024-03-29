﻿using Rhino.Geometry;
using Robots;
using static Extensions.GeometryUtil;

namespace Extensions.Toolpaths;

public class OrientToolpath
{
    public IToolpath Toolpath { get; set; }

    public OrientToolpath(IToolpath toolpath, Mesh guide, Vector3d alignment, Mesh surface = null)
    {
        Toolpath = toolpath.ShallowClone();

        if (Toolpath.Targets is not IList<Target> targets)
            throw new ArgumentException(" Targets of toolpath should be a list.");

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];

            if (target is CartesianTarget)
            {
                var copyTarget = target.ShallowClone() as CartesianTarget;
                var plane = copyTarget.Plane;
                var normal = OrientToMesh(plane.Origin, guide, surface);
                var newPlane = AlignedPlane(plane.Origin, normal, alignment);
                copyTarget.Plane = newPlane;
                targets[i] = copyTarget;
            }
        }
    }
}
