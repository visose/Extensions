using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.IO;

namespace Extensions.Model.Discrete
{
    public class Assembly
    {
        public List<Tile> Tiles { get; set; }
        public List<Instance> Instances { get; set; }
        public float AngleLimit { get; set; }
        public float BreakForce { get; set; }

        private Assembly() { }

        public static void Export(List<string> blockNames, string instanceLayerName, double angleLimit, double breakForce, string fileName, RhinoDoc doc)
        {
            var definitions = blockNames.Select(n => doc.InstanceDefinitions.First(i => i.Name == n));
            var instanceLayer = doc.Layers.FindName(instanceLayerName);
            var assembly = new Assembly()
            {
                Tiles = definitions.Select(d => new Tile(d, doc)).ToList(),
                Instances = definitions
                         .SelectMany(d => d.GetReferences(1).Where(i => i.Attributes.LayerIndex == instanceLayer.Index))
                         .Select(d => new Instance(d.InstanceDefinition.Index, d.InstanceXform))
                         .ToList(),
                AngleLimit = (float)angleLimit,
                BreakForce = (float)breakForce
            };

            var serializer = new XmlSerializer(typeof(Assembly));
            using (var writer = XmlWriter.Create(fileName))
            {
                serializer.Serialize(writer, assembly);
            }
        }
    }

    public class Tile
    {
        public int Index { get; set; }
        public float Mass { get; set; }
        public Vector3Export Centroid { get; set; }
        public List<ExportMesh> Renderers { get; set; }
        public List<ExportMesh> Colliders { get; set; }
        public List<Orient> Faces { get; set; }

        private Tile() { }

        public Tile(InstanceDefinition definition, RhinoDoc doc)
        {
            Index = definition.Index;

              var geometry = definition.GetObjects();

            int renderIndex = doc.Layers.FindName("Renders").Index;

            var renderMeshes = geometry
                      .Where(g => g.Attributes.LayerIndex == renderIndex)
                      .Select(g => g.Geometry as Mesh)
                      .ToList();
  
            Renderers = renderMeshes.Select(m => new ExportMesh(m)).ToList();

            int collisionsIndex = doc.Layers.FindName("Collisions").Index;

            var meshColliders = geometry
                 .Where(g => g.Attributes.LayerIndex == collisionsIndex)
                 .Select(g => g.Geometry as Mesh)
                 .ToList();

            Colliders = meshColliders
                .Select(m => m.Offset(0.001))
                 .Select(m => new ExportMesh(m))
                 .ToList();

            Point3d centroid = Point3d.Origin;
            double mass = 0.0;
            double density = 2400 * 0.5;

            foreach (var mesh in meshColliders)
            {
                var prop = VolumeMassProperties.Compute(mesh);
                double elementMass = prop.Volume * density;
                centroid += prop.Centroid * elementMass;
                mass += elementMass;
            }

            centroid /= mass;
            Centroid = new Vector3Export(centroid);
            Mass = (float)mass;

            int facesIndex = doc.Layers.FindName("Faces").Index;

            Faces = geometry
                     .Where(g => g.Attributes.LayerIndex == facesIndex)
                     .Select(g =>
                      {
                          Curve curve = g.Geometry as Curve;
                          curve.TryGetPolyline(out Polyline pl);
                          var plane = new Plane(pl[1], pl[2], pl[0]);
                          plane.Origin = (pl[0] * 0.5 + pl[2] * 0.5);
                          return new Orient(plane);
                      }).ToList();
        }
    }

    public class Instance
    {
        public int DefinitionIndex;
        public Orient Orient;

        private Instance() { }

        public Instance(int definitionIndex, Transform transform)
        {
            DefinitionIndex = definitionIndex;

            var plane = Plane.WorldXY;
            plane.Transform(transform);
            Orient = new Orient(plane);
        }
    }

    public class ExportMesh
    {
        public List<Vector3Export> Vertices { get; set; }
        public List<Vector2Export> TextureCoordinates { get; set; }
        public List<int> Faces { get; set; }

        private ExportMesh() { }

        public ExportMesh(Mesh mesh)
        {
            mesh.Faces.ConvertQuadsToTriangles();

            Vertices = mesh.Vertices.Select(p => new Vector3Export(p)).ToList();
            TextureCoordinates = mesh.TextureCoordinates.Select(p => new Vector2Export(p)).ToList();
            Faces = mesh.Faces.SelectMany(f => new int[] { f.C, f.B, f.A }).ToList();
        }
    }

    [XmlType(TypeName = "Vector3")]
    public struct Vector3Export
    {
        public float x, y, z;

        public Vector3Export(Point3d point)
        {
            x = (float)point.X;
            y = (float)point.Z;
            z = (float)point.Y;
        }

        public Vector3Export(Point3f point)
        {
            x = point.X;
            y = point.Z;
            z = point.Y;
        }
    }

    [XmlType(TypeName = "Vector2")]
    public struct Vector2Export
    {
        public float x, y;

        public Vector2Export(Point2f point)
        {
            x = point.X;
            y = point.Y;
        }
    }

    public struct Orient
    {
        public Vector3Export Center;
        public QuaternionExport Rotation;

        public Orient(Plane plane)
        {
            Center = new Vector3Export(plane.Origin);
            Rotation = new QuaternionExport(plane);
        }
    }

    [XmlType(TypeName = "Quaternion")]
    public struct QuaternionExport
    {
        public float x, y, z, w;

        public QuaternionExport(Plane plane)
        {
            var q = Quaternion.Rotation(Plane.WorldXY, plane);
            w = (float)-q.A;
            x = (float)q.B;
            y = (float)q.D;
            z = (float)q.C;
        }
    }
}
