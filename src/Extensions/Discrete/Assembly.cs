using System.Xml;
using System.Xml.Serialization;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace Extensions.Discrete;

public class Assembly
{
    public List<Tile> Tiles { get; set; } = [];
    public List<Instance> Instances { get; set; } = [];
    public float AngleLimit { get; set; }
    public float BreakForce { get; set; }

    Assembly() { }

    public static void Export(IReadOnlyList<string> blockNames, string instanceLayerName, double density, double angleLimit, double breakForce, string fileName, RhinoDoc doc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(density, nameof(density));

        var definitions = blockNames
            .Select(name => doc.InstanceDefinitions.FirstOrDefault(definition => definition.Name == name)
                ?? throw new ArgumentException($"Block '{name}' was not found.", nameof(blockNames)))
            .ToArray();
        var instanceLayer = doc.Layers.FindName(instanceLayerName)
            ?? throw new ArgumentException($"Layer '{instanceLayerName}' was not found.", nameof(instanceLayerName));

        Assembly assembly = new()
        {
            Tiles = definitions.Select(d => new Tile(d, density, doc)).ToList(),
            Instances = definitions
                     .SelectMany(d => d.GetReferences(1).Where(i => i.Attributes.LayerIndex == instanceLayer.Index))
                     .Select(d => new Instance(d.InstanceDefinition.Index, d.InstanceXform))
                     .ToList(),
            AngleLimit = (float)angleLimit,
            BreakForce = (float)breakForce
        };

        XmlSerializer serializer = new(typeof(Assembly));
        using var writer = XmlWriter.Create(fileName);
        serializer.Serialize(writer, assembly);
    }
}

public class Tile
{
    public int Index { get; set; }
    public float Mass { get; set; }
    public Vector3Export Centroid { get; set; }
    public List<MeshExport> Renderers { get; set; } = [];
    public List<MeshExport> Colliders { get; set; } = [];
    public List<Vector3Export> Faces { get; set; } = [];

    Tile() { }

    public Tile(InstanceDefinition definition, double density, RhinoDoc doc)
    {
        Index = definition.Index;

        var geometry = definition.GetObjects();

        int renderIndex = doc.Layers.FindName("Render")?.Index
            ?? throw new InvalidOperationException("Layer 'Render' was not found.");

        var renderMeshes = geometry
                  .Where(g => g.Attributes.LayerIndex == renderIndex)
                  .Select(g => g.Geometry)
                  .OfType<Mesh>()
                  .ToList();

        Renderers = renderMeshes.Select(m => new MeshExport(m)).ToList();

        int collisionsIndex = doc.Layers.FindName("Collision")?.Index
            ?? throw new InvalidOperationException("Layer 'Collision' was not found.");

        var meshColliders = geometry
             .Where(g => g.Attributes.LayerIndex == collisionsIndex)
             .Select(g => g.Geometry)
             .OfType<Mesh>()
             .ToList();

        Colliders = meshColliders
            .Select(m => m.Offset(0.001))
             .Select(m => new MeshExport(m))
             .ToList();

        Point3d centroid = Point3d.Origin;
        double mass = 0.0;

        foreach (var mesh in meshColliders)
        {
            var prop = VolumeMassProperties.Compute(mesh)
                ?? throw new InvalidOperationException("Could not compute mass properties for collision mesh.");

            double elementMass = prop.Volume * density;
            centroid += prop.Centroid * elementMass;
            mass += elementMass;
        }

        ArgumentOutOfRangeException.ThrowIfZero(mass, nameof(mass));

        centroid /= mass;
        Centroid = new(centroid);
        Mass = (float)mass;

        int facesIndex = doc.Layers.FindName("Faces")?.Index
            ?? throw new InvalidOperationException("Layer 'Faces' was not found.");

        Faces = geometry
                 .Where(g => g.Attributes.LayerIndex == facesIndex)
                 .Select(g => g.Geometry)
                 .OfType<Point>()
                 .Select(point => new Vector3Export(point.Location))
                 .ToList();
    }
}

public class Instance
{
    public int DefinitionIndex;
    public Pose Pose;

    Instance() { }

    public Instance(int definitionIndex, Transform transform)
    {
        DefinitionIndex = definitionIndex;

        var plane = Plane.WorldXY;
        plane.Transform(transform);
        Pose = new(plane);
    }
}

public class MeshExport
{
    public List<Vector3Export> Vertices { get; set; } = [];
    public List<Vector2Export> TextureCoordinates { get; set; } = [];
    public List<int> Faces { get; set; } = [];

    MeshExport() { }

    public MeshExport(Mesh mesh)
    {
        var exportMesh = mesh.DuplicateMesh();
        exportMesh.Faces.ConvertQuadsToTriangles();

        Vertices = exportMesh.Vertices.Select(p => new Vector3Export(p)).ToList();
        TextureCoordinates = exportMesh.TextureCoordinates.Select(p => new Vector2Export(p)).ToList();
        Faces = exportMesh.Faces.SelectMany(f => new int[] { f.C, f.B, f.A }).ToList();
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
public struct Vector2Export(Point2f point)
{
    public float x = point.X, y = point.Y;
}

public struct Pose(Plane plane)
{
    public Vector3Export position = new(plane.Origin);
    public QuaternionExport rotation = new(plane);
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
