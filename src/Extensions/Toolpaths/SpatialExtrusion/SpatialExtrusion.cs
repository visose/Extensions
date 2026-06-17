using Rhino.Geometry;
using Robots;

namespace Extensions.Toolpaths.SpatialExtrusion;

public class SpatialExtrusion
{
    readonly List<Target> _targets = [];
    readonly List<(Line segment, int type)> _display = [];

    public IReadOnlyList<Target> Targets => _targets;
    public IReadOnlyList<(Line segment, int type)> Display => _display;

    public SpatialExtrusion(IReadOnlyList<Polyline> polylines, SpatialAttributes attributes)
    {
        ArgumentOutOfRangeException.ThrowIfZero(polylines.Count, nameof(polylines));

        CreateTargets(polylines, attributes);
    }

    void CreateTargets(IReadOnlyList<Polyline> polylines, SpatialAttributes attributes)
    {
        List<GeometryBase> environment = [.. attributes.Environment];
        var meshes = environment.OfType<Mesh>()
                     .Select(m => (mesh: m, height: m.GetBoundingBox(true).Max.Z)).ToList();

        environment.RemoveAll(g => g is Mesh);

        foreach (var polyline in polylines)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(polyline.Count, 2, nameof(polylines));

            double height = polyline.BoundingBox.Min.Z;

            var meshesToAdd = meshes.Where(m => m.height < (height + 1.0)).Select(m => m.mesh).ToList();
            bool hasToWait = meshesToAdd.Count > 0;

            for (int i = meshes.Count - 1; i >= 0; i--)
            {
                if (meshesToAdd.Contains(meshes[i].mesh))
                    meshes.RemoveAt(i);
            }

            environment.AddRange(meshesToAdd);

            List<Vertex> vertices = new(polyline.Count);

            for (int i = 0; i < polyline.Count; i++)
                new Vertex(i, polyline[i], vertices, attributes, environment);

            foreach (var vertex in vertices) vertex.Initialize();

            foreach (var vertex in vertices) _targets.AddRange(vertex.GetTargets(hasToWait));

            foreach (var vertex in vertices) _display.Add(vertex.GetDisplay());

            environment.Add(polyline.ToNurbsCurve());
        }
    }
}
