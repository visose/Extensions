using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Extensions.Model.Discrete
{
    public static class CurveSnap
    {
        readonly static Sphere _unitSphere;

        static CurveSnap()
        {
            _unitSphere = new Sphere(Point3d.Origin, 1);
        }

        public enum SnapType { Equirectangular, Icosahedral, Quadrangular }

        public static Polyline SnapCurve(Curve curve, double length, SnapType snapType = SnapType.Equirectangular, int divisions = 0)
        {
            var snapVectors = SnapVectors();

            var ts = curve.DivideByLength(length, true, out Point3d[] points);
            Point3d next = points[0];
            var polyline = new Point3d[ts.Length];
            polyline[0] = next;

            for (int i = 0; i < ts.Length - 1; i++)
            {
                var tangent = points[i + 1] - next;
                var snappedVector = VectorSnap(tangent);
                double segmentLength = tangent.Length;

                next += snappedVector * segmentLength;
                polyline[i + 1] = next;
            }

            return new Polyline(polyline);


            IList<Vector3f> SnapVectors()
            {
                switch (snapType)
                {
                    case SnapType.Equirectangular:
                        divisions += 1; divisions *= 2;
                        return Mesh.CreateFromSphere(_unitSphere, divisions * 2, divisions).Normals;
                    case SnapType.Icosahedral:
                        return Mesh.CreateIcoSphere(_unitSphere, divisions).Normals;
                    case SnapType.Quadrangular:
                        return Mesh.CreateQuadSphere(_unitSphere, divisions).Normals;
                    default:
                        throw new NotSupportedException(" Snap type not supported.");
                }
            }

            Vector3d VectorSnap(Vector3d vector)
            {
                double minAngle = double.MaxValue;
                Vector3d minVector = Vector3d.Unset;

                foreach (var v in snapVectors)
                {
                    var angle = Vector3d.VectorAngle(vector, v);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        minVector = v;
                    }
                }

                return minVector;
            }
        }
    }
}
