using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Model.Toolpaths.Milling
{
    public class EndMill
    {
        public enum Geometry { Flat, Ball }

        public double Diameter { get; set; }
        public double Length { get; set; }
        public double CutLength { get; set; }
        public Geometry Nose { get; set; }
    }

    public class MillingAttributes
    {
        public Target ReferenceTarget { get; set; }

        public EndMill EndMill { get; set; }
        public double StepOver { get; set; }
        public double StepDown { get; set; }

        public double SafeZOffset { get; set; }

        public Speed SafeSpeed { get; set; }
        public Speed PlungeSpeed { get; set; }
        public Speed CutSpeed { get; set; }

        public Zone SafeZone { get; set; }
        public Zone PlungeZone { get; set; }
        public Zone CutZone { get; set; }

        public MillingAttributes(Target referenceTarget, EndMill endMill, double stepOver, double stepDown, double safeZOffset, double safeSpeed, double plungeSpeed, double cutSpeed, double cutZone)
        {
            ReferenceTarget = referenceTarget;
            EndMill = endMill;
            StepOver = stepOver;
            StepDown = stepDown;
            SafeZOffset = safeZOffset;

            SafeSpeed = new Speed(name: "SafeSpeed", translation: safeSpeed);
            PlungeSpeed = new Speed(name: "PlungeSpeed", translation: plungeSpeed);
            CutSpeed = new Speed(name: "CutSpeed", translation: cutSpeed);

            SafeZone = new Zone(endMill.Diameter, "SafeZone");
            PlungeZone = new Zone(0, "PlungeZone");
            CutZone = new Zone(cutZone, "CutZone");
        }
    }
}