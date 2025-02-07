using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using VECS;

namespace COMP302.MousaHussein
{
    public static class MousaHussein
    {
        public static HalfEdgeCu[] BuildHalfEdges(Mesh mesh)
        {
            return BuildHalfEdges(mesh.Vertices, mesh.Faces);
        }

        public static HalfEdgeCu[] BuildHalfEdges(Vertex[] vertices, Vector3Int[] triangles)
        {
            TriangleCu[] cuTriangles = new TriangleCu[triangles.Length];
            HalfEdgeCu[] cuHalfEdges = new HalfEdgeCu[triangles.Length * 3];

            // stores a list referencing each half edge used by each vertex
            List<int>[] incidentHalfEdgesForEachVertex = new List<int>[vertices.Length];

            // construct initial half edge data
            // NOTE the outer loop can be parallelised but isn't for now
            for (int i = 0; i < triangles.Length; i++)
            {
                for(int k = 0; k < 3; k++)
                {
                    cuHalfEdges[3 * i + k].triangle = (uint)i;
                    cuHalfEdges[3 * i + k].next = (uint)(3 * i + (k + 1) % 3);
                    cuHalfEdges[3 * i + k].opposite = -1;
                    cuHalfEdges[3 * i + k].vertex = (uint)triangles[i][k];
                }
                cuTriangles[i].halfEdge = (uint)(3 * i);
            }

            //Build a list of incident halfedges foreach vertex
            for (int i = 0; i < cuHalfEdges.Length; i++)
            {
                incidentHalfEdgesForEachVertex[cuHalfEdges[i].vertex] ??= [];
                incidentHalfEdgesForEachVertex[cuHalfEdges[i].vertex].Add(i);
            }

            // set the opposite half edge index for each half edge
            // NOTE the outer loop can be parallelised but isn't for now
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = 0; j < incidentHalfEdgesForEachVertex[i].Count; j++)
                {
                    for (int k = 0; k < incidentHalfEdgesForEachVertex[i].Count && k != j; k++)
                    {
                        var h1 = incidentHalfEdgesForEachVertex[i][j];
                        var h2 = incidentHalfEdgesForEachVertex[i][k];
                        if (triangles[cuHalfEdges[h1].triangle].Contains((int)cuHalfEdges[cuHalfEdges[h2].next].vertex))
                        {
                            cuHalfEdges[h1].opposite = (int)cuHalfEdges[h2].next;
                            cuHalfEdges[cuHalfEdges[h2].next].opposite = h1;
                        }
                    }
                }
            }

            Console.WriteLine("WARN: cuTriangles array is constructed but is never used");

            return cuHalfEdges;
        }
    }
}
