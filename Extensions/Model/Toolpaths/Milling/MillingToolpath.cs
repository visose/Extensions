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
    class MillingToolpath
    {
        readonly MillingAttributes _att;
        readonly Tool _tool;
        readonly BoundingBox _bbox;

        public List<Target> Targets { get; set; } = new List<Target>();
        public List<int> SubPrograms { get; set; } = new List<int>();

        public MillingToolpath(IList<Polyline> paths, BoundingBox box, MillingAttributes attributes)
        {
            _att = attributes;
            _bbox = box;
            _tool = CreateTool();
            CreateTargets(paths);
        }

        void CreateTargets(IList<Polyline> paths)
        {
            Point3d current = Point3d.Unset;

            var safeZ = _bbox.Max.Z + _att.SafeZOffset;

            foreach (var path in paths)
            {
                var first = path[0];
                var last = path[path.Count - 1];

                var firstSafe = new Point3d(first.X, first.Y, safeZ);
                Targets.Add(CreateTarget(firstSafe, _att.SafeSpeed, _att.SafeZone));

                var firstOffset = first + Vector3d.ZAxis * _att.SafeZOffset;
                Targets.Add(CreateTarget(firstOffset, _att.SafeSpeed, _att.SafeZone));

                Targets.Add(CreateTarget(first, _att.PlungeSpeed, _att.PlungeZone));

                for (int i = 1; i < path.Count - 1; i++)
                    Targets.Add(CreateTarget(path[i], _att.CutSpeed, _att.CutZone));

                Targets.Add(CreateTarget(last, _att.CutSpeed, _att.PlungeZone));

                var lastOffset = last + Vector3d.ZAxis * _att.SafeZOffset;
                Targets.Add(CreateTarget(lastOffset, _att.PlungeSpeed, _att.PlungeZone));

                var lastSafe = new Point3d(last.X, last.Y, safeZ);
                Targets.Add(CreateTarget(lastSafe, _att.SafeSpeed, _att.SafeZone));

                SubPrograms.Add(Targets.Count);
            }
        }

        Tool CreateTool()
        {
            var refTool = _att.ReferenceTarget.Tool;
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
            var frame = _att.ReferenceTarget.Frame;
            var target = new CartesianTarget(plane, null, Target.Motions.Linear, _tool, speed, zone, null, frame, null);
            return target;
        }
    }
}
