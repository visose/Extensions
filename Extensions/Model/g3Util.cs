using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using static System.Math;

namespace Extensions.Model
{
    static class g3Util
    {
        public static Point3d ToRhinoPoint(this g3.Vector3d v)
        {
            return new Point3d(v.x, v.y, v.z);
        }

        public static Vector3d ToRhinoVector(this g3.Vector3d v)
        {
            return new Vector3d(v.x, v.y, v.z);
        }

    }
}
