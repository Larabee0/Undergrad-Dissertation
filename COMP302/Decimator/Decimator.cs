using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using UnityMesh = VECS.Mesh;

namespace COMP302.Decimator
{
    public sealed class TargetConditions
    {
        /// <summary>
        /// Target count of triangles
        /// </summary>
        public int faceCount;

        /// <summary>
        /// Target count of vertices
        /// </summary>
        public int vertexCount;

        /// <summary>
        /// Max operations of collapsions
        /// </summary>
        public int maxOperations;

        /// <summary>
        /// Max error of collapsion
        /// </summary>
        public float maxMetrix;

        /// <summary>
        /// Max time of optimzation
        /// </summary>
        public float maxTime;

        public TargetConditions()
        {
            faceCount = 0;
            vertexCount = 0;
            maxOperations = 0;
            maxMetrix = 0;
            maxTime = 0;
        }
    }

    public sealed class UnityMeshDecimation
    {

        private const int PRINT_FREQUENCY = 200;
        private const int HEAP_RATIO = 4;

        private Mesh _mesh;
        private BinaryHeap<float, EdgeCollapse> _heap;
        private BVH<Face> _bvh;
        private TargetConditions _targetConditions;

        private int _initVertexCount;
        private int _initFaceCount;
        private float _currMetric;
        private int _currOperations;

        private bool _showProgress;


        private Stopwatch _stopWatch;
        private Stopwatch StopWatch
        {
            get
            {
                _stopWatch ??= new Stopwatch();
                return _stopWatch;
            }
        }

        #region API
        public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetConditions, bool showProgress = false)
        {
            _showProgress = showProgress;
            InitializeMesh(mesh, collapseParam, targetConditions);
            OptimizeMesh();
        }

        public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, int targetTriangles, float targetMetric, bool showProgress = false)
        {
            var targetOptions = new TargetConditions()
            {
                faceCount = targetTriangles,
                maxMetrix = targetMetric,
            };
            Execute(mesh, collapseParam, targetOptions, showProgress);
        }

        public void ToMesh(UnityMesh m)
        {
            _mesh?.ToMesh(m);
        }

        public Face GetSelectedFace(Vector3 start, Vector3 end)
        {
            var hit = _bvh.Traverse((b) => {
                return MeshUtil.IsLineInBox(start, end, b);
            });

            Face selected = null;
            float minD = float.MaxValue;
            for (int h = 0; h < hit.Count; h++)
            {
                var faces = hit[h].GObjects;
                if (faces == null)
                {
                    continue;
                }
                for (int i = 0; i < faces.Count; i++)
                {
                    var face = faces[i];
                    if (MeshUtil.IsLineIntersectTriangle(start, end, face.P(0), face.P(1), face.P(2), out Vector3 result))
                    {
                        Vector3 center = (face.P(0) + face.P(1) + face.P(2)) / 3;
                        float d = (center - start).LengthSquared();
                        if (d < minD)
                        {
                            minD = d;
                            selected = face;
                        }
                    }
                }
            }
            return selected;
        }

        public EdgeCollapse[] GetFaceCollapse(Face face)
        {
            var collapse = new List<EdgeCollapse>();
            using (var tEnu = _heap.GetEnumerator())
            {
                while (tEnu.MoveNext())
                {
                    var edge = tEnu.Current;
                    var v0 = edge.v0;
                    var v1 = edge.v1;
                    if ((v0 == face.V(0) && v1 == face.V(1)) || (v0 == face.V(1) && v1 == face.V(0)) ||
                        (v0 == face.V(0) && v1 == face.V(2)) || (v0 == face.V(2) && v1 == face.V(0)) ||
                        (v0 == face.V(1) && v1 == face.V(2)) || (v0 == face.V(2) && v1 == face.V(1)))
                    {
                        collapse.Add(edge);
                    }
                }
            }
            return [.. collapse];
        }
        #endregion

        #region Internal Methods
        private void InitializeMesh(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetOptions)
        {
            StopWatch.Restart();

            _targetConditions = targetOptions;
            _mesh = new Mesh(mesh);
            _heap = new BinaryHeap<float, EdgeCollapse>(HEAP_RATIO * _mesh.FaceCount, float.MinValue, float.MaxValue);
            _bvh = new BVH<Face>(new BVHFaceAdapter(), collapseParam.PreventIntersection ? _mesh.faces : []);

            _mesh.InitIMark();
            EdgeCollapse.globalMark = 0;
            EdgeCollapse.Init(_mesh, _heap, _bvh, collapseParam);

            _initVertexCount = _mesh.VertexCount;
            _initFaceCount = _mesh.FaceCount;
            _currMetric = _heap.First.Priority();

            _stopWatch.Stop();
            Console.WriteLine($"<color=cyan>Initialization time: {_stopWatch.ElapsedMilliseconds / 1000f}</color>");

        }

        private void OptimizeMesh()
        {
            StopWatch.Restart();

            _currOperations = 0;
            while (!IsGoalReached() && _heap.Count > 0)
            {
                var locMod = _heap.Dequeue();
                _currMetric = locMod.Priority();

                if (locMod.IsUpToDate() && locMod.IsFeasible())
                {
                    try
                    {
                        if (locMod.Execute())
                        {
                            locMod.UpdateHeap();
                            _currOperations++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        break;
                    }
                    if (_currOperations % PRINT_FREQUENCY == 0)
                    {
                        var status = GetCurrentStatus();
                        Console.WriteLine(status);
                    }
                }
            }
            Console.WriteLine(GetCurrentStatus());

            StopWatch.Stop();
            Console.WriteLine($"<color=cyan>Optimization time: {StopWatch.ElapsedMilliseconds / 1000f}</color>");
            Console.WriteLine($"<color=lime>Original Face: {_initFaceCount}, Final Face: {_mesh.FaceCount}, Ratio: {_mesh.FaceCount * 100 / _initFaceCount}%</color>");

        }

        private bool IsGoalReached()
        {
            if (_targetConditions.faceCount > 0 && _mesh.FaceCount <= _targetConditions.faceCount) return true;
            if (_targetConditions.vertexCount > 0 && _mesh.VertexCount <= _targetConditions.vertexCount) return true;
            if (_targetConditions.maxOperations > 0 && _currOperations > _targetConditions.maxOperations) return true;
            if (_targetConditions.maxMetrix > 0 && _currMetric > _targetConditions.maxMetrix) return true;
            if (_targetConditions.maxTime > 0 && _stopWatch.ElapsedMilliseconds > _targetConditions.maxTime * 1000) return true;
            return false;
        }

        private string GetCurrentStatus()
        {
            return $"vert: {_mesh.VertexCount} face: {_mesh.FaceCount} bvh size: {_bvh.nodeCount} heap size: {_heap.Count} error: {_currMetric}";
        }
        #endregion
    }
}
