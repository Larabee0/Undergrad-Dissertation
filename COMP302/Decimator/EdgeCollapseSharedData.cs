using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace COMP302.Decimator
{
    public class EdgeCollapseSharedData
    {

        private readonly Vertex dummyVert = new(pos: new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        private readonly Dictionary<Vertex, int> vertCnt = [];
        private readonly Dictionary<(Vertex, Vertex), int> edgeCnt = [];
        private readonly List<Vertex>[] boundaryVertexVec = [[], []];
        private readonly List<Vertex> LkEdge = [];
        public int GlobalMark;
        public int PropertySize;
        public int QuadricSize;

        public QuadricHelper QH;

        public static void Init(Mesh mesh, BinaryHeap<float, EdgeCollapse> heap, BVH<Face> bvh, EdgeCollapseParameter param)
        {
            var data = new EdgeCollapseSharedData(mesh,param);
            data.InitQuadric(mesh, param);
            data.InitCollapses(mesh, heap, bvh, param);
        }

        private EdgeCollapseSharedData(Mesh mesh,  EdgeCollapseParameter param)
        {
            param = (EdgeCollapseParameter)param.Clone();
            param.UsedProperty &= mesh.Properties;
            if (param.OptimalSampleCount <= 0) param.OptimalSampleCount = 1;

            PropertySize = Face.GetPropertySize(param.UsedProperty);
            QuadricSize = 3 + PropertySize; // position + other properties

            mesh.BuildVertexFace();
            mesh.BuildFaceBorder();

            if (param.PreserveBoundary)
            {
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    var face = mesh.Faces[i];
                    if (!face.IsDeleted() && face.IsWritable())
                    {
                        for (int j = 0; j < Face.VERTEX_COUNT; j++)
                        {
                            if (face.IsBorder(j))
                            {
                                if (face.V(j).IsWritable)
                                {
                                    face.V(j).ClearWritable();
                                }
                                if (face.V1(j).IsWritable)
                                {
                                    face.V1(j).ClearWritable();
                                }
                            }
                        }
                    }
                }
            }

        }

        private void InitQuadric(Mesh mesh, EdgeCollapseParameter param)
        {
            QH = new(mesh.Verts);

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var face = mesh.Faces[i];
                if (face.IsDeleted())
                {
                    continue;
                }
                Quadric q = new(QuadricSize);
                q.ByFace(face, QH.Qd3(face.V(0)), QH.Qd3(face.V(1)), QH.Qd3(face.V(2)), param.QualityQuadric, param.BoundaryWeight, param.UsedProperty);

                for (int j = 0; j < Face.VERTEX_COUNT; j++)
                {
                    var vert = face.V(j);
                    var props = face.GetPropertyS(param.UsedProperty, j);
                    if (vert.IsWritable)
                    {
                        if (!QH.Contains(vert, props))
                        {
                            QH.Alloc(vert, props);
                        }
                        QH.SumAll(vert, props, q);
                    }
                }
            }
        }

        private void InitCollapses(Mesh mesh, BinaryHeap<float, EdgeCollapse> heap, BVH<Face> bvh, EdgeCollapseParameter param)
        {
            heap.Clear();
            // exclude face with different sub mesh ?
            for (int i = 0; i < mesh.Verts.Count; i++)
            {
                var vertex = mesh.Verts[i];
                if (vertex.IsWritable)
                {
                    var vfi = new VFIterator(vertex);
                    while (vfi.MoveNext())
                    {
                        vfi.V1.ClearVisited();
                        vfi.V2.ClearVisited();
                    }
                    vfi.Reset();
                    while (vfi.MoveNext())
                    {
                        if (vfi.V0 < vfi.V1 && vfi.V1.IsWritable && !vfi.V1.IsVisited)
                        {
                            vfi.V1.SetVisited();
                            var collapse = new EdgeCollapse(this, new VertexPair(vfi.V0, vfi.V1), mesh, heap, bvh, param);
                            heap.Enqueue(collapse, collapse.Priority());
                        }
                        if (vfi.V0 < vfi.V2 && vfi.V2.IsWritable && !vfi.V2.IsVisited)
                        {
                            vfi.V2.SetVisited();
                            var collapse = new EdgeCollapse(this, new VertexPair(vfi.V0, vfi.V2), mesh, heap, bvh, param);
                            heap.Enqueue(collapse, collapse.Priority());
                        }
                    }
                }
            }
        }


        public bool LinkConditions(VertexPair pair)
        {
            // at the end of the loop each vertex must be counted twice
            // except for boundary vertex.
            vertCnt.Clear();
            edgeCnt.Clear();

            // the list of the boundary vertexes for the two endpoints
            boundaryVertexVec[0].Clear();
            boundaryVertexVec[1].Clear();

            static void TryDicAdd<T>(Dictionary<T, int> dic, T key, int value)
            {
                if (!dic.ContainsKey(key)) dic[key] = value;
                else dic[key] += value;
            }

            // Collect vertexes and edges of V0 and V1
            VFIterator vfi;
            (Vertex, Vertex) e;
            for (int i = 0; i < 2; i++)
            {
                vfi = new VFIterator(i == 0 ? pair.V0 : pair.V1);
                while (vfi.MoveNext())
                {
                    var v1 = vfi.V1;
                    var v2 = vfi.V2;
                    TryDicAdd(vertCnt, v1, 1);
                    TryDicAdd(vertCnt, v2, 1);

                    e = v1 < v2 ? (v1, v2) : (v2, v1);
                    TryDicAdd(edgeCnt, e, 1);
                }
                // Now a loop to add dummy stuff: add the dummy vertex and two dummy edges
                // (and remember to increase the counters for the two boundary vertexes involved)
                foreach (var vcmit in vertCnt)
                {
                    if (vcmit.Value == 1)
                    { // boundary vertexes are counted only once
                        boundaryVertexVec[i].Add(vcmit.Key);
                    }
                }
                if (boundaryVertexVec[i].Count == 2)
                {
                    // aha! one of the two vertex of the collapse is on the boundary
                    // so add dummy vertex and two dummy edges
                    TryDicAdd(vertCnt, dummyVert, 2);
                    TryDicAdd(edgeCnt, (dummyVert, boundaryVertexVec[i][0]), 1);
                    TryDicAdd(edgeCnt, (dummyVert, boundaryVertexVec[i][1]), 1);

                    // remember to hide the boundaryness of the two boundary vertexes
                    TryDicAdd(vertCnt, boundaryVertexVec[i][0], 1);
                    TryDicAdd(vertCnt, boundaryVertexVec[i][1], 1);
                }
            }

            // Final loop to find cardinality of Lk( V0-V1 )
            // Note that Lk(edge) is only a set of vertices.
            LkEdge.Clear();

            vfi = new VFIterator(pair.V0);
            while (vfi.MoveNext())
            {
                if (vfi.V1 == pair.V1) LkEdge.Add(vfi.V2);
                if (vfi.V2 == pair.V1) LkEdge.Add(vfi.V1);
            }

            // if the collapsing edge was a boundary edge, we must add the dummy vertex.
            // Note that this implies that Lk(edge) >=2;
            if (LkEdge.Count == 1)
            {
                LkEdge.Add(dummyVert);
            }

            // NOW COUNT!!!
            int sharedEdgeCnt = 0;
            foreach (var eci in edgeCnt)
            {
                if (eci.Value == 2) sharedEdgeCnt++;
            }
            if (sharedEdgeCnt > 0) return false;

            int sharedVertCnt = 0;
            foreach (var vci in vertCnt)
            {
                if (vci.Value == 4) sharedVertCnt++;
            }

            if (sharedVertCnt != LkEdge.Count) return false;

            return true;
        }
    }
}
