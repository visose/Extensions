using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Model.Toolpaths.Extrusion
{

    public class ExtrusionAttributes
    {
        public double NozzleDiameter { get; set; }
        public double LayerHeight { get; set; }
        public double BeadWidth { get; set; }

        public double SafeZOffset { get; set; }

        public Speed SafeSpeed { get; set; }
        public Speed ApproachSpeed { get; set; }
        public Speed ExtrusionSpeed { get; set; }

        public Zone SafeZone { get; set; }
        public Zone ApproachZone { get; set; }
        public Zone ExtrusionZone { get; set; }

        public Tool Tool { get; set; }
        public Frame Frame { get; set; }
        public double[] Home { get; set; }


        public ExtrusionAttributes Initialize()
        {
            SafeSpeed = SafeSpeed.CloneWithName<Speed>(nameof(SafeSpeed));
            ApproachSpeed = ApproachSpeed.CloneWithName<Speed>(nameof(ApproachSpeed));
            ExtrusionSpeed = ExtrusionSpeed.CloneWithName<Speed>(nameof(ExtrusionSpeed));

            SafeZone = SafeZone.CloneWithName<Zone>(nameof(SafeZone));
            ApproachZone = ApproachZone.CloneWithName<Zone>(nameof(ApproachZone));
            ExtrusionZone = ExtrusionZone.CloneWithName<Zone>(nameof(ExtrusionZone));

            Frame = Frame.CloneWithName<Frame>(nameof(Frame));
            BeadWidth = Util.GetWidth(NozzleDiameter, LayerHeight);

            return this;
        }
    }

}