using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace Extensions.Model.Spatial
{
    public interface IPositionable
    {
        Point3d Position { get; }
    }

    public struct Vector3i
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
            X = point.X > 0 ? (int)point.X : (int)point.X - 1;
            Y = point.Y > 0 ? (int)point.Y : (int)point.Y - 1;
            Z = point.Z > 0 ? (int)point.Z : (int)point.Z - 1;
        }

        public override bool Equals(object obj)
        {
            Vector3i other = (Vector3i)obj;

            return (X == other.X && Y == other.Y && Z == other.Z);
        }


        // Lifted from Geometry3Sharp
        public override int GetHashCode()
        {
            //return 137 * X + 149 * Y + 163 * Z;
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 16777619) ^ X.GetHashCode();
                hash = (hash * 16777619) ^ Y.GetHashCode();
                hash = (hash * 16777619) ^ Z.GetHashCode();
                return hash;
            }
        }
    }

    public struct Vector2i
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
            X = point.X > 0 ? (int)point.X : (int)point.X - 1;
            Y = point.Y > 0 ? (int)point.Y : (int)point.Y - 1;
        }
    }
}
