using g3;
using Rhino.Geometry;

namespace Extensions.Geometry;

public static class Remesh
{
    public static DMesh3 ToDMesh3(this Mesh mesh)
    {
        var dMesh3 = new DMesh3();

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

    public static Mesh ToRhinoMesh(this DMesh3 dMesh3)
    {
        dMesh3 = new DMesh3(dMesh3, true, MeshComponents.All);
        var mesh = new Mesh();

        var vertices = dMesh3.Vertices().Select(v => new Point3d(v.x, v.z, v.y));
        var faces = dMesh3.Triangles().Select(f => new MeshFace(f.a, f.b, f.c));

        mesh.Vertices.AddVertices(vertices);
        mesh.Faces.AddFaces(faces);
        mesh.Normals.ComputeNormals();
        mesh.Compact();

        return mesh;
    }

    public static Mesh RemeshTest(Mesh inMesh, double fResScale = 1.0, int iterations = 50)
    {
        inMesh.Faces.ConvertQuadsToTriangles();
        DMesh3 mesh = inMesh.ToDMesh3();
        mesh.CheckValidity();
        AxisAlignedBox3d bounds = mesh.CachedBounds;

        // construct mesh projection target
        DMesh3 meshCopy = new(mesh);
        meshCopy.CheckValidity();
        DMeshAABBTree3 tree = new(meshCopy);
        tree.Build();
        MeshProjectionTarget target = new()
        {
            Mesh = meshCopy,
            Spatial = tree
        };

        // construct constraint set
        MeshConstraints cons = new();

        //EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip | EdgeRefineFlags.NoCollapse;
        EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;

        foreach (int eid in mesh.EdgeIndices())
        {
            double fAngle = MeshUtil.OpeningAngleD(mesh, eid);
            if (fAngle > 30.0)
            {
                cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));
                Index2i ev = mesh.GetEdgeV(eid);
                int nSetID0 = (mesh.GetVertex(ev[0]).y > bounds.Center.y) ? 1 : 2;
                int nSetID1 = (mesh.GetVertex(ev[1]).y > bounds.Center.y) ? 1 : 2;
                cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true, nSetID0));
                cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true, nSetID1));
            }
        }

        Remesher r = new(mesh);
        r.Precompute();
        r.SetExternalConstraints(cons);
        r.SetProjectionTarget(target);

        r.EnableFlips = r.EnableSplits = r.EnableCollapses = true;
        r.MinEdgeLength = 0.5 * fResScale;
        r.MaxEdgeLength = 1.0 * fResScale;
        r.EnableSmoothing = true;
        r.SmoothSpeedT = 0.5;

        try
        {
            for (int k = 0; k < iterations; ++k)
            {
                r.BasicRemeshPass();
                // mesh.CheckValidity();
            }
        }
        catch
        {
            // ignore
        }

        return mesh.ToRhinoMesh();
    }
}
