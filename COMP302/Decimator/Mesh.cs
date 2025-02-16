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

        public List<Vertex> Verts { get; private set; }
        public List<Face> Faces { get; private set; }

        public int VertexCount { get; private set; }
        public int FaceCount { get; private set; }
        public int[] UvSizes { get; private set; }

        public VertexProperty Properties => (VertexProperty)_flags;

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
            Faces = [];
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

                    if (HasFlag((int)VertexProperty.Normal)) face.Normals[k] = normals[index];
                    if (HasFlag((int)VertexProperty.UV0)) face.Uvs[k] = uvs[index];
                }
                face.BuildFaceNormal();
                Faces.Add(face);
            }
            Verts = [.. vertDic.Values];
            FaceCount = Faces.Count;
            VertexCount = Verts.Count;
        }

        public void ToMesh(VECS.Mesh dstMesh)
        {
            var dic = new Dictionary<Vector<float>, uint>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var subMeshes = new List<uint>();

            for (int i = 0; i < Faces.Count; i++)
            {
                var face = Faces[i];
                if (!face.IsDeleted())
                {
                    for (int j = 0; j < Face.VERTEX_COUNT; j++)
                    {
                        var key = face.GetPropertyS((VertexProperty)_flags, j);
                        if (!dic.TryGetValue(key, out uint idx))
                        {
                            vertices.Add(face.V(j).Pos);
                            if (HasFlag((int)VertexProperty.Normal)) normals.Add(face.Normals[j]);
                            if (HasFlag((int)VertexProperty.UV0)) uvs.Add(face.Uvs[j]);
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
            for (int i = 0; i < Verts.Count; i++)
            {
                if (!Verts[i].IsDeleted)
                {
                    Verts[i].InitIMark();
                }
            }
        }

        public void BuildVertexFace()
        {
            for (int i = 0; i < Verts.Count; i++)
            {
                Verts[i].VfParent = null;
                Verts[i].VfIndex = 0;
            }
            for (int i = 0; i < Faces.Count; i++)
            {
                var face = Faces[i];
                if (!face.IsDeleted())
                {
                    for (int j = 0; j < Face.VERTEX_COUNT; j++)
                    {
                        face.VfParent[j] = face.V(j).VfParent;
                        face.VfIndex[j] = face.V(j).VfIndex;
                        face.V(j).VfParent = face;
                        face.V(j).VfIndex = j;
                    }
                }
            }
        }

        public void BuildFaceBorder()
        {
            for (int i = 0; i < Faces.Count; i++)
            {
                Faces[i].ClearBorderFlags();
            }
            int[] borderFlags = [(int)Face.FaceFlags.Border0, (int)Face.FaceFlags.Border1, (int)Face.FaceFlags.Border2];
            for (int i = 0; i < Verts.Count; i++)
            {
                var vertex = Verts[i];
                if (!vertex.IsDeleted)
                {
                    var vfi = new VFIterator(vertex);
                    while (vfi.MoveNext())
                    {
                        vfi.F.V1(vfi.Z).ClearVisited();
                        vfi.F.V2(vfi.Z).ClearVisited();
                    }
                    vfi.Reset();
                    while (vfi.MoveNext())
                    {
                        if (vfi.F.V1(vfi.Z).IsVisited) vfi.F.V1(vfi.Z).ClearVisited();
                        else vfi.F.V1(vfi.Z).SetVisited();
                        if (vfi.F.V2(vfi.Z).IsVisited) vfi.F.V2(vfi.Z).ClearVisited();
                        else vfi.F.V2(vfi.Z).SetVisited();
                    }
                    vfi.Reset();
                    while (vfi.MoveNext())
                    {
                        if (vfi.F.V(vfi.Z) < vfi.F.V1(vfi.Z) && vfi.F.V1(vfi.Z).IsVisited)
                        {
                            vfi.F.AddFlag(borderFlags[vfi.Z]);
                        }
                        if (vfi.F.V(vfi.Z) < vfi.F.V2(vfi.Z) && vfi.F.V2(vfi.Z).IsVisited)
                        {
                            vfi.F.AddFlag(borderFlags[(vfi.Z + 2) % 3]);
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
