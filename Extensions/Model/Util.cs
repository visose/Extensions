using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using static System.Math;

namespace Extensions.Model
{
    public static class Util
    {
        public const double HalfPI = PI * 0.5;
        public const double PI2 = PI * 2;
        public const double Tol = 0.001;
        public const double UnitTol = 1E-08;
        public const double DegreeToRadian = PI / 180.0;

        public static double ToRadians(this double degree)
        {
            return degree * DegreeToRadian;
        }

        public static double GetWidth(double diameter, double height)
        {
            double r = diameter * 0.5;
            return ((r * r) / (height * 0.5)) * 2;
        }

        public static double Snap(double number, double interval)
        {
            number /= interval;
            number = Round(number);
            number *= interval;
            return number;
        }
    }
}