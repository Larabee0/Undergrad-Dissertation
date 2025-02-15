using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace COMP302.Decimator
{
    [Flags]
    public enum VertexProperty : int
    {
        Position = 1 << 1,
        Normal = 1 << 2,
        Tangent = 1 << 3,
        Color = 1 << 4,
        UV0 = 1 << 5,
        UV1 = 1 << 6,
        UV2 = 1 << 7,
        UV3 = 1 << 8,
        UV4 = 1 << 9,
        UV5 = 1 << 10,
        UV6 = 1 << 11,
        UV7 = 1 << 12,
        BoneWeight = 1 << 13,
    }

    public class Mesh : FlagBase
    {
        public const int UV_COUNT = 8;

        public List<Vertex> verts { get; private set; }
        public List<Face> faces { get; private set; }

        public int VertexCount { get; private set; }
        public int FaceCount { get; private set; }
        public int[] UvSizes { get; private set; }

        public VertexProperty properties => (VertexProperty)flags;

        public Mesh(VECS.Mesh mesh)
        {
            var VECSVertices = mesh.Vertices;
            var vertices = new Vector3[mesh.VertexCount];
            var normals = new Vector3[mesh.VertexCount];
            var uvs = new Vector2[mesh.VertexCount];

            AddFlag((int)VertexProperty.Position);
            if (normals.Length > 0) AddFlag((int)VertexProperty.Normal);
            if (uvs.Length > 0) AddFlag((int)VertexProperty.UV0);
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                vertices[i] = VECSVertices[i].Position;
                normals[i] = VECSVertices[i].Normal;
                uvs[i] = new(VECSVertices[i].Elevation, VECSVertices[i].BiomeSelect);
            }

            var vertDic = new Dictionary<Vector3, Vertex>();
            faces = [];
            var indices = mesh.Indices;

            for (int j = 0; j < indices.Length; j += 3)
            {
                var face = new Face();
                for (int k = 0; k < Face.VERTEX_COUNT; k++)
                {
                    var index = indices[j + k];
                    var v = vertices[index];
                    if (!vertDic.TryGetValue(v, out Vertex vert))
                    {
                        vert = new Vertex(v);
                        vertDic[v] = vert;
                    }
                    face.V(k) = vert;

                    if (HasFlag((int)VertexProperty.Normal)) face.normals[k] = normals[index];
                    if (HasFlag((int)VertexProperty.UV0)) face.uvs[k] = uvs[index];
                }
                face.BuildFaceNormal();
                faces.Add(face);
            }
            verts = [.. vertDic.Values];
            FaceCount = faces.Count;
            VertexCount = verts.Count;
        }

        public void ToMesh(VECS.Mesh dstMesh)
        {
            var dic = new Dictionary<Vector<float>, uint>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var subMeshes = new List<uint>();

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                if (!face.IsDeleted())
                {
                    for (int j = 0; j < Face.VERTEX_COUNT; j++)
                    {
                        var key = face.GetPropertyS((VertexProperty)flags, j);
                        if (!dic.TryGetValue(key, out uint idx))
                        {
                            vertices.Add(face.V(j).pos);
                            if (HasFlag((int)VertexProperty.Normal)) normals.Add(face.normals[j]);
                            if (HasFlag((int)VertexProperty.UV0)) uvs.Add(face.uvs[j]);
                            idx = (uint)vertices.Count - 1;
                            dic.Add(key, idx);
                        }
                        subMeshes.Add(idx);
                    }
                }
            }

            VECS.Vertex[] VECSvertices = new VECS.Vertex[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                VECSvertices[i] = new()
                {
                    Position = vertices[i],
                    Normal = normals[i],
                    Elevation = uvs[i].X,
                    BiomeSelect = uvs[i].Y,
                };

            }

            dstMesh.Vertices = VECSvertices;

            dstMesh.Indices = [.. subMeshes];
        }

        public void InitIMark()
        {
            for (int i = 0; i < verts.Count; i++)
            {
                if (!verts[i].IsDeleted())
                {
                    verts[i].InitIMark();
                }
            }
        }

        public void BuildVertexFace()
        {
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i].vfParent = null;
                verts[i].vfIndex = 0;
            }
            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                if (!face.IsDeleted())
                {
                    for (int j = 0; j < Face.VERTEX_COUNT; j++)
                    {
                        face.vfParent[j] = face.V(j).vfParent;
                        face.vfIndex[j] = face.V(j).vfIndex;
                        face.V(j).vfParent = face;
                        face.V(j).vfIndex = j;
                    }
                }
            }
        }

        public void BuildFaceBorder()
        {
            for (int i = 0; i < faces.Count; i++)
            {
                faces[i].ClearBorderFlags();
            }
            int[] borderFlags = [(int)Face.FaceFlags.Border0, (int)Face.FaceFlags.Border1, (int)Face.FaceFlags.Border2];
            for (int i = 0; i < verts.Count; i++)
            {
                var vertex = verts[i];
                if (!vertex.IsDeleted())
                {
                    var vfi = new VFIterator(vertex);
                    while (vfi.MoveNext())
                    {
                        vfi.f.V1(vfi.z).ClearVisited();
                        vfi.f.V2(vfi.z).ClearVisited();
                    }
                    vfi.Reset();
                    while (vfi.MoveNext())
                    {
                        if (vfi.f.V1(vfi.z).IsVisited()) vfi.f.V1(vfi.z).ClearVisited();
                        else vfi.f.V1(vfi.z).SetVisited();
                        if (vfi.f.V2(vfi.z).IsVisited()) vfi.f.V2(vfi.z).ClearVisited();
                        else vfi.f.V2(vfi.z).SetVisited();
                    }
                    vfi.Reset();
                    while (vfi.MoveNext())
                    {
                        if (vfi.f.V(vfi.z) < vfi.f.V1(vfi.z) && vfi.f.V1(vfi.z).IsVisited())
                        {
                            vfi.f.AddFlag(borderFlags[vfi.z]);
                        }
                        if (vfi.f.V(vfi.z) < vfi.f.V2(vfi.z) && vfi.f.V2(vfi.z).IsVisited())
                        {
                            vfi.f.AddFlag(borderFlags[(vfi.z + 2) % 3]);
                        }
                    }
                }
            }
        }

        public static void DeleteFace(Mesh m, Face f)
        {
            f.SetDeleted();
            m.FaceCount--;
        }

        public static void DeleteVertex(Mesh m, Vertex v)
        {
            v.SetDeleted();
            m.VertexCount--;
        }
    }
}
