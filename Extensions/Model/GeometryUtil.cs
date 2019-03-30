using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using static System.Math;

namespace Extensions.Model
{
    static class GeometryUtil
    {
        public static Vector3d PolarToVector(double a, double b)
        {
            var c = a;
            a = b;
            b = c;

            var n = Vector3d.Zero;
            n.X = Sin(a) * Cos(b);
            n.Y = Sin(a) * Sin(b);
            n.Z = Cos(a);

            return n;
        }

        public static Plane AlignedPlane(Point3d position, Vector3d normal, Vector3d alignment)
        {
            if (!alignment.IsUnitVector)
            {
                alignment = (Point3d)alignment - position;
                alignment.Unitize();
                alignment.Rotate(Util.HalfPI, normal);
            }

            var plane = new Plane(position, normal);
            var alignAngle = Vector3d.VectorAngle(plane.XAxis, alignment, plane);
            plane.Rotate(alignAngle, plane.Normal);

            return plane;
        }
    }
}
