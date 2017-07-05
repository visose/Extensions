using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using Robots.Commands;

namespace Extensions.Toolpaths
{
    internal class SpatialAttributes
    {
        public double Diameter { get; set; }
        public double VerticalOffset { get; set; }
        public double RotationOffset { get; set; }
        public double DistancePlunge { get; set; }
        public double DistanceAhead { get; set; }
        public double DistanceHorizontal { get; set; }

        public CartesianTarget ReferenceTarget { get; set; }

        public Speed Approach { get; set; }
        public Speed Plunge { get; set; }
        public Speed Fast { get; set; }
        public Speed Medium { get; set; }
        public Speed Slow { get; set; }

        public Command StopExtrusion { get; set; }
        public Command FastExtrusion { get; set; }
        public Command MediumExtrusion { get; set; }
        public Command SlowExtrusion { get; set; }

        public Command LongWait { get; set; }
        public Command ShortWait { get; set; }

        public Command AheadCommand { get; set; }

        public SpatialAttributes(IList<double> variables, CartesianTarget target, IList<double> speeds, IList<double> waits, IList<int> dos)
        {
            if (variables.Count != 6) throw new Exception(" There must be 6 variables.");
            if (speeds.Count != 5) throw new Exception(" There must be 5 speeds.");
            if (waits.Count != 4) throw new Exception(" There must be 4 wait times.");
            if (dos.Count != 2) throw new Exception(" There must be 2 digital outputs.");

            Diameter = variables[0];
            DistancePlunge = variables[1];
            VerticalOffset = variables[2];
            RotationOffset = variables[3];
            DistanceAhead = variables[4];
            DistanceHorizontal = variables[5];

            ReferenceTarget = target;

            Approach = new Speed(name: "Approach", translation: speeds[0]);
            Plunge = new Speed(name: "Plunge", translation: speeds[1]);
            Fast = new Speed(name: "FastExtrusion", translation: speeds[2]);
            Medium = new Speed(name: "MediumExtrusion", translation: speeds[3]);
            Slow = new Speed(name: "SlowExtrusion", translation: speeds[4]);

            var waitAfterStart = new Wait(waits[0]);

            StopExtrusion = new Group() { new SetDO(dos[0], false), new SetDO(dos[1], false)};
            FastExtrusion = new Group() { new SetDO(dos[0], true), new SetDO(dos[1], false), waitAfterStart };
            MediumExtrusion = new Group() { new SetDO(dos[0], false), new SetDO(dos[1], true), waitAfterStart };
            SlowExtrusion = new Group() { new SetDO(dos[0], true), new SetDO(dos[1], true), waitAfterStart };

            LongWait = new Group() { StopExtrusion, new Wait(waits[3]) };
            ShortWait = new Group() { StopExtrusion, new Wait(waits[2]) };

            AheadCommand = new Group() { StopExtrusion, new Wait(waits[1]) };
        }

    }
}