using g3;
using Rhino.Geometry;

namespace Extensions.Geometry;

public static class Remesh
{
    extension(Mesh mesh)
    {
        public DMesh3 ToDMesh3()
        {
            if (mesh.Normals.Count != mesh.Vertices.Count)
            {
                mesh = mesh.DuplicateMesh();
                mesh.Normals.ComputeNormals();
            }

            DMesh3 dMesh3 = new();

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var vertex = mesh.Vertices[i];
                var normal = mesh.Normals[i];

                NewVertexInfo ni = new()
                {
                    v = new g3.Vector3d(vertex.X, vertex.Z, vertex.Y),
                    n = new g3.Vector3f(normal.X, normal.Z, normal.Y)
                };

                dMesh3.AppendVertex(ni);
            }

            foreach (var face in mesh.Faces)
            {
                dMesh3.AppendTriangle(face.A, face.B, face.C);
            }

            return dMesh3;
        }
    }

    extension(DMesh3 dMesh3)
    {
        public Mesh ToRhinoMesh()
        {
            dMesh3 = new(dMesh3, true, MeshComponents.All);
            Mesh mesh = new();

            var vertices = dMesh3.Vertices().Select(v => new Point3d(v.x, v.z, v.y));
            var faces = dMesh3.Triangles().Select(f => new MeshFace(f.a, f.b, f.c));

            mesh.Vertices.AddVertices(vertices);
            mesh.Faces.AddFaces(faces);
            mesh.Normals.ComputeNormals();
            mesh.Compact();

            return mesh;
        }
    }

    public static Mesh Create(Mesh mesh, double targetLength = 1.0, int iterations = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetLength);
        ArgumentOutOfRangeException.ThrowIfNegative(iterations);

        var source = mesh.DuplicateMesh();
        source.Faces.ConvertQuadsToTriangles();
        DMesh3 dMesh = source.ToDMesh3();
        dMesh.CheckValidity();
        AxisAlignedBox3d bounds = dMesh.CachedBounds;

        DMesh3 referenceMesh = new(dMesh);
        referenceMesh.CheckValidity();
        DMeshAABBTree3 tree = new(referenceMesh);
        tree.Build();
        MeshProjectionTarget target = new()
        {
            Mesh = referenceMesh,
            Spatial = tree
        };

        MeshConstraints cons = new();

        EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;

        foreach (int eid in dMesh.EdgeIndices())
        {
            double angle = MeshUtil.OpeningAngleD(dMesh, eid);
            if (angle > 30.0)
            {
                cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));
                Index2i vertices = dMesh.GetEdgeV(eid);
                int firstSetId = dMesh.GetVertex(vertices[0]).y > bounds.Center.y ? 1 : 2;
                int secondSetId = dMesh.GetVertex(vertices[1]).y > bounds.Center.y ? 1 : 2;
                cons.SetOrUpdateVertexConstraint(vertices[0], new VertexConstraint(true, firstSetId));
                cons.SetOrUpdateVertexConstraint(vertices[1], new VertexConstraint(true, secondSetId));
            }
        }

        Remesher remesher = new(dMesh);
        remesher.Precompute();
        remesher.SetExternalConstraints(cons);
        remesher.SetProjectionTarget(target);

        remesher.EnableFlips = remesher.EnableSplits = remesher.EnableCollapses = true;
        remesher.MinEdgeLength = 0.5 * targetLength;
        remesher.MaxEdgeLength = targetLength;
        remesher.EnableSmoothing = true;
        remesher.SmoothSpeedT = 0.5;

        for (int i = 0; i < iterations; ++i)
        {
            remesher.BasicRemeshPass();
        }

        return dMesh.ToRhinoMesh();
    }
}
