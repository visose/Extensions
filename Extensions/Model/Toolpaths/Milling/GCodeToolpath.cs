using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Rhino.Geometry;
using Robots;
using gs;
using static Extensions.Model.GeometryUtil;

namespace Extensions.Model.Toolpaths.Milling
{
    public class GCodeToolpath : IToolpath
    {
        public IEnumerable<Target> Targets { get; private set; }
        public FiveAxisToRobots Toolpath { get; set; }

        public GCodeToolpath(string file, CartesianTarget referenceTarget, Vector3d alignment)
        {
            using var reader = File.OpenText(file);

            var parser = new GenericGCodeParser();
            var code = parser.Parse(reader);

            Toolpath = new FiveAxisToRobots(referenceTarget, alignment, code);
            Targets = Toolpath.Targets;
        }

        public IToolpath ShallowClone()
        {
            var toolpath = MemberwiseClone() as GCodeToolpath;
            toolpath.Targets = Toolpath.Targets.ToList();
            return toolpath;
        }
    }

    public class FiveAxisToRobots
    {
        public List<Target> Targets { get; set; } = new List<Target>();
        List<string> _ignored = new List<string>();
        List<int> _RapidStarts = new List<int>() { 0 };
        Tool _tool;
        Frame _mcs;

        Dictionary<(GCodeLine.LType letter, int number), Action<GCodeLine>> _gCodeMap;
        Dictionary<double, Speed> _speeds = new Dictionary<double, Speed>();
        CartesianTarget _refTarget;
        Vector3d _alignment;
        int _lastRapid = 0;

        public void Deconstruct(out Tool tool, out Frame mcs, out List<int> rapidStarts, out List<string> ignored)
        {
            tool = _tool;
            mcs = _mcs;
            rapidStarts = _RapidStarts;
            ignored = _ignored;
        }

        internal FiveAxisToRobots(CartesianTarget refTarget, Vector3d alignment, GCodeFile file)
        {
            _refTarget = refTarget;
            _alignment = alignment;

            var constructionPlane = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetConstructionPlane().Plane;
            constructionPlane.Origin = Point3d.Origin;
            var workPlane = _refTarget.Frame.Plane;
            var xform = Transform.PlaneToPlane(Plane.WorldXY, workPlane);
            constructionPlane.Transform(xform);

            _mcs = new Frame(plane: constructionPlane, name: "MCS");

            _gCodeMap = new Dictionary<(GCodeLine.LType letter, int number), Action<GCodeLine>>
            {
                { (GCodeLine.LType.GCode, 0), RapidMove },
                { (GCodeLine.LType.GCode, 1), LinearMove},
                { (GCodeLine.LType.MCode, 6), ToolSet },
             };

            Interpret(file);
        }

        void Interpret(GCodeFile file)
        {
            foreach (GCodeLine line in file.AllLines())
            {
                if (_gCodeMap.TryGetValue((line.type, line.code), out var action))
                    action(line);
                else
                    Ignore(line);
            }
        }

        void Ignore(GCodeLine line)
        {
            string message = $"{line.lineNumber}: {line.orig_string}";
            _ignored.Add(message);
        }

        void Move(GCodeLine line)
        {
            var parameters = new[] { "X", "Y", "Z", "A", "B", "F" };
            var v = new double[6];

            for (int i = 0; i < 6; i++)
            {
                if (!GCodeUtil.TryFindParamNum(line.parameters, parameters[i], ref v[i]))
                {
                    Ignore(line);
                    return;
                }
            }

            var p = new Point3d(v[0], v[1], v[2]);

            var a = v[3].ToRadians();
            var b = v[4].ToRadians();

            var n = PolarToVector(a, b);
            var plane = AlignedPlane(p, n, _alignment);

            Speed speed;

            if (Targets.Count == 0)
            {
                speed = _refTarget.Speed;
            }
            else
            {
                var feed = v[5];

                if (!_speeds.TryGetValue(feed, out speed))
                {
                    speed = _refTarget.Speed.CloneWithName<Speed>($"Feed{_speeds.Count:000}");
                    speed.TranslationSpeed = feed / 60.0;
                    _speeds.Add(feed, speed);
                }
            }

            var target = new CartesianTarget(
                plane,
                null,
                Motions.Linear,
                _tool,
                speed,
                _refTarget.Zone,
                Command.Default,
                _mcs,
                null
                );

            Targets.Add(target);
        }

        void RapidMove(GCodeLine line)
        {
            Move(line);

            int i = line.lineNumber;
            if ((i > 0) && (i - _lastRapid > 1))
            {
                _RapidStarts.Add(i);
            }

            _lastRapid = i;
        }

        void LinearMove(GCodeLine line)
        {
            Move(line);
        }

        void ToolSet(GCodeLine line)
        {
            double length = 60;
            double diameter = 10;
            GCodeUtil.TryFindParamNum(line.parameters, "L", ref length);
            GCodeUtil.TryFindParamNum(line.parameters, "D", ref diameter);

            var endMill = new EndMill()
            {
                Length = length,
                Diameter = diameter,
            };

            _tool = endMill.MakeTool(_refTarget.Tool);
        }
    }
}
