using Rhino.Geometry;
using Robots;

namespace Extensions.Toolpaths.SpatialExtrusion;

public class SpatialExtrusion
{
    public List<Target> Targets { get; } = new List<Target>();
    public List<(Line segment, int type)> Display { get; } = new List<(Line segment, int type)>();

    public SpatialExtrusion(IEnumerable<Polyline> polylines, SpatialAttributes attributes)
    {
        CreateTargets(polylines.ToList(), attributes);
    }

    void CreateTargets(IEnumerable<Polyline> polylines, SpatialAttributes attributes)
    {
        var meshes = attributes.Environment.OfType<Mesh>()
                     .Select(m => (mesh: m, height: m.GetBoundingBox(true).Max.Z)).ToList();

        attributes.Environment.RemoveAll(g => g is Mesh);

        foreach (var polyline in polylines)
        {
            double height = polyline.BoundingBox.Min.Z;

            var meshesToAdd = meshes.Where(m => m.height < (height + 1.0)).Select(m => m.mesh).ToList();
            bool hasToWait = meshesToAdd.Count > 0;

            for (int i = meshes.Count - 1; i >= 0; i--)
            {
                if (meshesToAdd.Contains(meshes[i].mesh))
                    meshes.RemoveAt(i);
            }

            attributes.Environment.AddRange(meshesToAdd);

            var vertices = new List<Vertex>(polyline.Count);

            for (int i = 0; i < polyline.Count; i++)
                new Vertex(i, polyline[i], vertices, attributes);

            foreach (var vertex in vertices) vertex.Initialize();
            foreach (var vertex in vertices) Targets.AddRange(vertex.GetTargets(hasToWait));
            foreach (var vertex in vertices) Display.Add(vertex.GetDisplay());

            attributes.Environment.Add(polyline.ToNurbsCurve());
        }
    }
}
