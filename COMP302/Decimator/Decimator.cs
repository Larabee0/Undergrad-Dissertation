using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using UnityMesh = VECS.DirectSubMesh;

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
        private const int HEAP_RATIO = 4;
        private Mesh _mesh;
        private BinaryHeap<float, EdgeCollapse> _heap;
        private BVH<Face> _bvh;
        private TargetConditions _targetConditions;

        private int _initVertexCount;
        private int _initFaceCount;
        private float _currMetric;
        private int _currOperations;

        private Stopwatch _stopWatch=new();
        #region API
        public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetConditions)
        {
            InitializeMesh(mesh, collapseParam, targetConditions);
            OptimizeMesh();
        }

        public void Execute(UnityMesh mesh, EdgeCollapseParameter collapseParam, int targetTriangles, float targetMetric)
        {
            var targetOptions = new TargetConditions()
            {
                faceCount = targetTriangles,
                maxMetrix = targetMetric,
            };
            Execute(mesh, collapseParam, targetOptions);
        }

        public void ToMesh(UnityMesh m)
        {
            _mesh?.ToMesh(m);
        }

        #endregion

        #region Internal Methods
        private void InitializeMesh(UnityMesh mesh, EdgeCollapseParameter collapseParam, TargetConditions targetOptions)
        {
            //StopWatch.Restart();

            _targetConditions = targetOptions;
            _mesh = new Mesh(mesh);
            _heap = new BinaryHeap<float, EdgeCollapse>(HEAP_RATIO * _mesh.FaceCount, float.MinValue, float.MaxValue);
            _bvh = new BVH<Face>(new BVHFaceAdapter(), collapseParam.PreventIntersection ? _mesh.Faces : []);

            _mesh.InitIMark();
            EdgeCollapseSharedData.Init(_mesh, _heap, _bvh, collapseParam);

            _initVertexCount = _mesh.VertexCount;
            _initFaceCount = _mesh.FaceCount;
            _currMetric = _heap.First.Priority();

            //_stopWatch.Stop();
            //Console.WriteLine($"<color=cyan>Initialization time: {_stopWatch.ElapsedMilliseconds / 1000f}</color>");

        }

        private void OptimizeMesh()
        {
            _stopWatch.Restart();

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
                    //if (_currOperations % PRINT_FREQUENCY == 0)
                    //{
                    //    var status = GetCurrentStatus();
                    //    Console.WriteLine(status);
                    //}
                }
            }
            //Console.WriteLine(GetCurrentStatus());

            _stopWatch.Stop();
            //Console.WriteLine($"<color=cyan>Optimization time: {StopWatch.ElapsedMilliseconds / 1000f}</color>");
            //Console.WriteLine($"<color=lime>Original Face: {_initFaceCount}, Final Face: {_mesh.FaceCount}, Ratio: {_mesh.FaceCount * 100 / _initFaceCount}%</color>");

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

        #endregion
    }
}
