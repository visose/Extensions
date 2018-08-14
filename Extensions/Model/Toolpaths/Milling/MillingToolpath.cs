using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Robots;
using Robots.Commands;
using static System.Math;
using static Extensions.Model.Util;

namespace Extensions.Model.Toolpaths.Milling
{
    class MillingToolpath : IToolpath
    {
        public IEnumerable<Target> Targets => _targets;
        public List<int> SubPrograms { get; set; } = new List<int>();

        readonly MillingAttributes _att;
        readonly Tool _tool;
        readonly BoundingBox _bbox;
        readonly List<Target> _targets = new List<Target>();

        public MillingToolpath(IList<Polyline> paths, BoundingBox box, MillingAttributes attributes)
        {
            _att = attributes;
            _bbox = box;
            _tool = CreateTool();
            CreateTargets(paths);
        }

        void CreateTargets(IList<Polyline> paths)
        {
            var safeZ = _bbox.Max.Z + _att.SafeZOffset;
            double layerZ = _att.StepDown + _att.SafeZOffset;
            _targets.Add(HomeStart());

            foreach (var path in paths)
            {
                var first = path[0];
                var last = path[path.Count - 1];

                var firstSafe = new Point3d(first.X, first.Y, safeZ);
                _targets.Add(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone));


                var firstOffset = first + Vector3d.ZAxis * layerZ;
                _targets.Add(CreateTarget(firstOffset, _att.SafeSpeed, _att.SafeZone));

                _targets.Add(CreateTarget(first, _att.PlungeSpeed, _att.PlungeZone));

                for (int i = 1; i < path.Count - 1; i++)
                    _targets.Add(CreateTarget(path[i], _att.CutSpeed, _att.CutZone));

                _targets.Add(CreateTarget(last, _att.CutSpeed, _att.PlungeZone));

                var lastOffset = last + Vector3d.ZAxis * layerZ;
                _targets.Add(CreateTarget(lastOffset, _att.PlungeSpeed, _att.PlungeZone));

                var lastSafe = new Point3d(last.X, last.Y, safeZ);
                _targets.Add(CreateTarget(lastSafe, _att.SafeSpeed, _att.SafeZone));

                SubPrograms.Add(_targets.Count);
            }

            _targets.Add(HomeEnd());
            SubPrograms.RemoveAt(SubPrograms.Count - 1);
        }

        Tool CreateTool()
        {
            var refTool = _att.Tool;
            var tcp = refTool.Tcp;
            bool translate = tcp.Translate(-tcp.Normal * _att.EndMill.Length);

            Mesh toolGeometry = refTool.Mesh.DuplicateMesh();
            Cylinder endMillCylinder = new Cylinder(new Circle(tcp, _att.EndMill.Diameter * 0.5), _att.EndMill.Length);
            Mesh endMillGeometry = Mesh.CreateFromCylinder(endMillCylinder, 1, 8);
            toolGeometry.Append(endMillGeometry);

            string name = $"{refTool.Name}_{_att.EndMill.Diameter:0}mm";

            var tool = new Tool(tcp, name, refTool.Weight, refTool.Centroid, toolGeometry);
            return tool;
        }


        Target CreateTarget(Point3d position, Speed speed, Zone zone = null)
        {
            var plane = Plane.WorldXY;
            plane.Origin = position;
            var frame = _att.Frame;
            var target = new CartesianTarget(plane, null, Target.Motions.Linear, _tool, speed, zone, null, frame, null);
            return target;
        }

        Target HomeStart()
        {
            var command = new Group()
                {
                    new Message("Press play to start milling..."),
                    new Stop()
                };

            var home = new JointTarget(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
            return home;
        }

        Target HomeEnd()
        {
            var command = new Group()
                {
                    new Message("Se acabó."),
                };

            var home = new JointTarget(_att.Home, _tool, _att.SafeSpeed, _att.SafeZone, command, _att.Frame);
            return home;
        }
    }
}
