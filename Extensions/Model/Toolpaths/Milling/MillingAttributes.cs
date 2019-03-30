using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Model.Toolpaths.Milling
{

    public class MillingAttributes
    {
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

        public Tool Tool { get; set; }
        public Frame Frame { get; set; }
        public double[] Home { get; set; }

        public MillingAttributes Initialize()
        {
            SafeSpeed = SafeSpeed.CloneWithName<Speed>(nameof(SafeSpeed));
            PlungeSpeed = PlungeSpeed.CloneWithName<Speed>(nameof(PlungeSpeed));
            CutSpeed = CutSpeed.CloneWithName<Speed>(nameof(CutSpeed));

            SafeZone = SafeZone.CloneWithName<Zone>(nameof(SafeZone));
            PlungeZone = PlungeZone.CloneWithName<Zone>(nameof(PlungeZone));
            CutZone = CutZone.CloneWithName<Zone>(nameof(CutZone));

            Frame = Frame.CloneWithName<Frame>(nameof(Frame));
            return this;
        }
    }
}