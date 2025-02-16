using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;
using PropertySetting = COMP302.Decimator.EdgeCollapseParameter.PropertySetting;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace COMP302.Decimator
{
    public class EdgeCollapse
    {
        private readonly EdgeCollapseSharedData _sharedData;
        private readonly Mesh _mesh;
        private readonly BinaryHeap<float, EdgeCollapse> _heap;
        private readonly BVH<Face> _bvh;
        private readonly EdgeCollapseParameter _param;
        private readonly VertexPair _pair;
        private float _priority;
        private readonly int _locakMark;

        public Vertex V0 => _pair.V0;
        public Vertex V1 => _pair.V1;

        public EdgeCollapse(
            EdgeCollapseSharedData sharedData,
            VertexPair pair,
            Mesh mesh,
            BinaryHeap<float, EdgeCollapse> heap,
            BVH<Face> bvh,
            EdgeCollapseParameter param)
        {
            _sharedData = sharedData;
            _pair = pair;
            _locakMark = _sharedData.GlobalMark;
            _mesh = mesh;
            _heap = heap;
            _bvh = bvh;
            _param = param;
            _priority = ComputePriority();
        }

        public float GetExtraWeight()
        {
            var weight = 0f;
            var lst = new List<(Vector<float>, Quadric)>[2] { _sharedData.QH.Vd(_pair.V0), _sharedData.QH.Vd(_pair.V1) };

            int index = 0;
            if ((_param.UsedProperty&VertexProperty.Normal)!=0)
            {
                var setting = _param.GetPropertySetting(VertexProperty.Normal);
                int size = CountDifference(lst, setting, (value) => {
                    return new Vector4(value[index], value[index + 1], value[index + 2],0);
                });
                weight += setting.ExtraWeight * (size - 2);
                index += 3;
            }
            if ((_param.UsedProperty&VertexProperty.UV0)!=0)
            {
                var setting = _param.GetPropertySetting(VertexProperty.UV0);
                int size = CountDifference(lst, setting, (value) => {
                    return  new Vector4(value[index], value[index + 1],0,0);
                });
                weight += setting.ExtraWeight * (size - 2); ;
                index += 2;
            }

            return weight;
        }

        public float ComputePriority()
        {
            Quadric qsum1 = new(_sharedData.QuadricSize);
            Quadric qsum2 = new(_sharedData.QuadricSize);
            Vector<double> min1 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            Vector<double> min2 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            Vector<float> property0_1 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property1_1 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property0_2 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property1_2 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            int nProperties = GetProperties(property0_1, property1_1, property0_2, property1_2);
            return ComputeMinimalsAndPriority(min1, min2, qsum1, qsum2, property0_1, property1_1, property0_2, property1_2, nProperties);
        }

        private void InterpolateProperties(
            Face face,
            int id,
            Face mface,
            int mid,
            Vector3 mcoord,
            Vector3 newPos,
            VertexProperty property)
        {
            Vector3 coord, closestCoord = Vector3.Zero;
            Vector3 p0 = face.P(0), p1 = face.P(1), p2 = face.P(2);
            coord = MeshUtil.BarycentricCoords(newPos, p0, p1, p2);
            bool outside = (coord.X < 0 || coord.Y < 0 || coord.Z < 0);
            if (outside) closestCoord = MeshUtil.PointToTriangle(p0, p1, p2, newPos).barycentric;

            if ((property & VertexProperty.Normal) != 0)
            {
                var setting = _param.GetPropertySetting(VertexProperty.Normal);
                face.Normals[id] =
                    (outside && setting.InterpolateWithAdjacentFace && Vector3.DistanceSquared(face.Normals[id], mface.Normals[mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateNormal(mcoord) :
                    (outside && setting.InterpolateClamped) ? face.InterpolateNormal(closestCoord) :
                    face.InterpolateNormal(coord);
            }
            if ((property & VertexProperty.UV0) != 0)
            {
                var setting = _param.GetPropertySetting(VertexProperty.UV0);
                face.Uvs[id] =
                        (outside && setting.InterpolateWithAdjacentFace && Vector2.DistanceSquared(face.Uvs[id], mface.Uvs[mid]) < setting.SqrDistanceThreshold) ? mface.InterpolateUV(mcoord) :
                        (outside && setting.InterpolateClamped) ? face.InterpolateUV(closestCoord) :
                        face.InterpolateUV(coord);
            }
        }

        private void InterpolateProperties(Vector3 newPos, VertexProperty property)
        {
            Vertex v0 = _pair.V0;
            Vertex v1 = _pair.V1;

            Face minf = null;
            int z0 = 0, z1 = 0;
            float minDistance = float.MaxValue;
            var vfi = new VFIterator(v0);
            while (vfi.MoveNext())
            {
                if (vfi.V1 == v1 || vfi.V2 == v1)
                {
                    var (_, _, sqrDistance) = MeshUtil.PointToTriangle(vfi.F.P(0), vfi.F.P(1), vfi.F.P(2), newPos);
                    if (sqrDistance < minDistance)
                    {
                        minDistance = sqrDistance;
                        minf = vfi.F;
                        z0 = vfi.Z;
                        z1 = (vfi.V1 == v1) ? (vfi.Z + 1) % 3 : (vfi.Z + 2) % 3;
                    }
                }
            }

            Vector3 mcoord = MeshUtil.BarycentricCoords(newPos, minf.P(0), minf.P(1), minf.P(2));
            if (mcoord.X < 0 || mcoord.Y < 0 || mcoord.Z < 0)
            {
                mcoord = MeshUtil.PointToTriangle(minf.P(0), minf.P(1), minf.P(2), newPos).barycentric;
            }

            vfi = new VFIterator(v0);
            while (vfi.MoveNext())
            {
                if (vfi.V1 == v1 || vfi.V2 == v1)
                {
                    continue;
                }
                InterpolateProperties(vfi.F, vfi.Z, minf, z0, mcoord, newPos, property);
            }
            vfi = new VFIterator(v1);
            while (vfi.MoveNext())
            {
                if (vfi.V1 == v0 || vfi.V2 == v0)
                {
                    continue;
                }
                InterpolateProperties(vfi.F, vfi.Z, minf, z1, mcoord, newPos, property);
            }
        }

        private int GetIntersectionCount(Vector3 newPos)
        {
            int count = 0;
            Vertex v0 = _pair.V0;
            Vertex v1 = _pair.V1;

            var set = new HashSet<Vector3>();
            var exclude = new HashSet<Face>();
            var t = new VFIterator(v0);
            while (t.MoveNext())
            {
                if (v0.Pos != newPos)
                {
                    if (t.V1 != v1)
                    {
                        set.Add(t.V1.Pos);
                    }
                    if (t.V2 != v1)
                    {
                        set.Add(t.V2.Pos);
                    }
                }
                exclude.Add(t.F);
            }
            t = new VFIterator(v1);
            while (t.MoveNext())
            {
                if (v1.Pos != newPos)
                {
                    if (t.V1 != v0)
                    {
                        set.Add(t.V1.Pos);
                    }
                    if (t.V2 != v0)
                    {
                        set.Add(t.V2.Pos);
                    }
                }
                exclude.Add(t.F);
            }
            var hit = _bvh.Traverse((b) => {
                foreach (var p in set)
                {
                    if (MeshUtil.IsLineInBox(newPos, p, b))
                    {
                        return true;
                    }
                }
                return false;
            });
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
                    if (!exclude.Contains(face))
                    {
                        foreach (var p in set)
                        {
                            if (MeshUtil.IsLineIntersectTriangle(newPos, p, face.P(0), face.P(1), face.P(2), out Vector3 result))
                            {
                                if (result != p && result != newPos)
                                {
                                    count++;
                                }
                            }
                        }
                    }
                }
            }
            return count;
        }

        public bool Execute()
        {
            Quadric qsum1 = new(_sharedData.QuadricSize);
            Quadric qsum2 = new(_sharedData.QuadricSize);
            Vector<double> min1 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            Vector<double> min2 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            Vector<float> property0_1 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property1_1 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property0_2 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> property1_2 = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> newProperty = Vector<float>.Build.Dense(_sharedData.PropertySize);
            Vector<float> newProperty2 = null;
            var qv = new List<(Vector<float>, Quadric)>();
            int nProperties;
            Vertex v0 = _pair.V0;
            Vertex v1 = _pair.V1;

            Quadric qsum3 = new(_sharedData.QH.Qd3(v0));
            qsum3.Add(_sharedData.QH.Qd3(v1));

            nProperties = GetProperties(property0_1, property1_1, property0_2, property1_2);

            ComputeMinimalsAndPriority(min1, min2, qsum1, qsum2, property0_1, property1_1, property0_2, property1_2, nProperties);

            Vector3 newPos = new((float)min1[0], (float)min1[1], (float)min1[2]);

            if (_param.PreventIntersection)
            {
                int intersects = GetIntersectionCount(newPos);
                if (intersects > 0)
                {
                    return false;
                }
            }

            VertexProperty prop = _mesh.Properties & ~VertexProperty.Position & ~_param.UsedProperty;
            InterpolateProperties(newPos, prop);

            var deleted = MeshUtil.DoCollapse(_mesh, _pair, newPos);
            if (_param.PreventIntersection)
            {
                for (int i = 0; i < deleted.Count; i++)
                {
                    _bvh.Remove(deleted[i]);
                }
            }

            for (int i = 0; i < _sharedData.PropertySize; i++)
            {
                newProperty[i] = (float)min1[3 + i];
            }
            Vector<float> newProperty1 = newProperty.Clone();
            qv.Add((newProperty1, new Quadric(qsum1)));

            if (nProperties > 1)
            {
                for (int i = 0; i < _sharedData.PropertySize; i++)
                {
                    newProperty[i] = (float)min2[3 + i];
                }
                newProperty2 = newProperty.Clone();
                qv.Add((newProperty2, new Quadric(qsum2)));
            }

            var vfi = new VFIterator(v1);
            while (vfi.MoveNext())
            {
                var property = vfi.F.GetPropertyS(_param.UsedProperty, vfi.Z);
                if (property.Equals(property0_1) || property.Equals(property1_1))
                {
                    vfi.F.SetPropertyS(_param.UsedProperty, vfi.Z, newProperty1);
                }
                else if (nProperties > 1 && (property.Equals(property0_2) || property.Equals(property1_2)))
                {
                    vfi.F.SetPropertyS(_param.UsedProperty, vfi.Z, newProperty2);
                }
                else
                {
                    // not in the edge, should do interploation ?
                    bool exist = false;
                    for (int i = 0; i < qv.Count; i++)
                    {
                        if (qv[i].Item1.Equals(property))
                        {
                            exist = true;
                            break;
                        }
                    }
                    if (!exist)
                    {
                        Quadric newq = null;
                        if (_sharedData.QH.Contains(v0, property))
                        {
                            newq = new Quadric(_sharedData.QH.Qd(v0, property));
                            newq.Sum3(_sharedData.QH.Qd3(v1), property);
                        }
                        else if (_sharedData.QH.Contains(v1, property))
                        {
                            newq = new Quadric(_sharedData.QH.Qd(v1, property));
                            newq.Sum3(_sharedData.QH.Qd3(v0), property);
                        }
                        qv.Add((property.Clone(), newq));
                    }
                }
                if (_param.PreventIntersection)
                {
                    _bvh.MarkForUpdate(vfi.F);
                }
            }
            if (_param.PreventIntersection)
            {
                _bvh.Optimize();
            }
            _sharedData.QH.Qd3(v1, qsum3);
            _sharedData.QH.Vd(v1, qv);
            return true;
        }

        public void UpdateHeap()
        {
            _sharedData.GlobalMark++;
            //var v0 = _pair.V0;
            var v1 = _pair.V1;
            v1.IMark = _sharedData.GlobalMark;

            VFIterator vfi = new(v1);
            while (vfi.MoveNext())
            {
                vfi.                V1.ClearVisited();
                vfi.                V2.ClearVisited();
            }

            vfi.Reset();
            while (vfi.MoveNext())
            {
                if (!vfi.V1.IsVisited && vfi.V1.IsWritable)
                {
                    vfi.                    V1.SetVisited();
                    var collapse = new EdgeCollapse(_sharedData, new VertexPair(vfi.V0, vfi.V1),  _mesh, _heap, _bvh, _param);
                    _heap.Enqueue(collapse, collapse.Priority());
                }

                if (!vfi.V2.IsVisited && vfi.V2.IsWritable)
                {
                    vfi.                    V2.SetVisited();
                    var collapse = new EdgeCollapse(_sharedData, new VertexPair(vfi.V0, vfi.V2),_mesh, _heap, _bvh, _param);
                    _heap.Enqueue(collapse, collapse.Priority());
                }
            }
        }

        private float ComputeMinimalsAndPriority(
            Vector<double> min1,
            Vector<double> min2,
            Quadric qsum1,
            Quadric qsum2,
            Vector<float> property0_1,
            Vector<float> property1_1,
            Vector<float> property0_2,
            Vector<float> property1_2,
            int nProperties)
        {

            var tmp1 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            var tmp2 = Vector<double>.Build.Dense(_sharedData.QuadricSize);
            float priority1, priority2;

            tmp1[0] = _pair.V0.Pos.X;
            tmp1[1] = _pair.V0.Pos.Y;
            tmp1[2] = _pair.V0.Pos.Z;
            for (int i = 0; i < property0_1.Count; i++)
            {
                tmp1[i + 3] = property0_1[i];
            }

            tmp2[0] = _pair.V1.Pos.X;
            tmp2[1] = _pair.V1.Pos.Y;
            tmp2[2] = _pair.V1.Pos.Z;
            for (int i = 0; i < property1_1.Count; i++)
            {
                tmp2[i + 3] = property1_1[i];
            }
            _sharedData.QH.Qd(_pair.V0, property0_1).CopyTo(qsum1);
            qsum1.Add(_sharedData.QH.Qd(_pair.V1, property1_1));

            ComputeMinimal(min1, tmp1, tmp2, qsum1);
            priority1 = ComputePropertyPriority(min1, qsum1);

            if (nProperties < 2)
            {
                return priority1 * (1 + GetExtraWeight());
            }

            for (int i = 0; i < property0_2.Count; i++)
            {
                tmp1[i + 3] = property0_2[i];
            }
            for (int i = 0; i < property1_2.Count; i++)
            {
                tmp2[i + 3] = property1_2[i];
            }

            _sharedData.QH.Qd(_pair.V0, property0_2).CopyTo(qsum2);
            qsum2.Add(_sharedData.QH.Qd(_pair.V1, property1_2));

            ComputeMinimal(min2, tmp1, tmp2, qsum2);
            priority2 = ComputePropertyPriority(min2, qsum2);

            if (priority1 > priority2)
            {
                ComputeMinimalWithGeoContraints(min2, tmp1, tmp2, qsum2, min1);
                priority2 = ComputePropertyPriority(min2, qsum2);
            }
            else
            {
                ComputeMinimalWithGeoContraints(min1, tmp1, tmp2, qsum1, min2);
                priority1 = ComputePropertyPriority(min1, qsum1);
            }

            _priority = MathF.Max(priority1, priority2) * (1 + GetExtraWeight());

            return _priority;
        }

        private int GetProperties(
            Vector<float> property0_1,
            Vector<float> property1_1,
            Vector<float> property0_2,
            Vector<float> property1_2)
        {
            int npropertys = 0;

            var vfi = new VFIterator(_pair.V0);
            while (vfi.MoveNext())
            {
                if (vfi.F.V(0) == _pair.V1 || vfi.F.V(1) == _pair.V1 || vfi.F.V(2) == _pair.V1)
                {
                    if (npropertys == 0)
                    {
                        vfi.F.GetPropertyS(_param.UsedProperty, MatchVertexID(vfi.F, _pair.V0)).CopyTo(property0_1);
                        vfi.F.GetPropertyS(_param.UsedProperty, MatchVertexID(vfi.F, _pair.V1)).CopyTo(property1_1);
                    }
                    else
                    {
                        vfi.F.GetPropertyS(_param.UsedProperty, MatchVertexID(vfi.F, _pair.V0)).CopyTo(property0_2);
                        vfi.F.GetPropertyS(_param.UsedProperty, MatchVertexID(vfi.F, _pair.V1)).CopyTo(property1_2);

                        if (property0_1.Equals(property0_2) && property1_1.Equals(property1_2))
                        {
                            return 1;
                        }
                        else
                        {
                            return 2;
                        }
                    }
                    npropertys++;
                }
            }
            return npropertys;
        }

        private void ComputeMinimal(
            Vector<double> vv,
            Vector<double> v0,
            Vector<double> v1,
            Quadric qsum)
        {
            double min = double.MaxValue;

            if (_param.OptimalPlacement)
            {
                bool rt = qsum.Minimum(vv);
                if (rt)
                {
                    return;
                }
            }

            double step = _param.OptimalPlacement ? (double)1 / (_param.OptimalSampleCount + 1) : 1;

            for (double t = 0; t <= 1; t += step)
            {
                var v = t * v1 + (1 - t) * v0;

                double q = qsum.Apply(v);
                if (q < min)
                {
                    min = q;
                    v.CopyTo(vv);
                }
            }
        }

        private float ComputePropertyPriority(
            Vector<double> vv,
            Quadric qsum)
        {

            Vertex v0 = _pair.V0;
            Vertex v1 = _pair.V1;

            Vector3 oldPos0 = v0.Pos;
            Vector3 oldPos1 = v1.Pos;

            v0.Pos = new Vector3((float)vv[0], (float)vv[1], (float)vv[2]);
            v1.Pos = v0.Pos;

            double quadErr = qsum.Apply(vv);

            double qt, minQual = double.MaxValue;
            double ndiff, minCos = double.MaxValue;

            VFIterator vfi = new(_pair.V0);
            while (vfi.MoveNext())
            {
                if (vfi.F.V(0) != v1 && vfi.F.V(1) != v1 && vfi.F.V(2) != v1)
                {
                    qt = vfi.F.GetQuality();
                    if (qt < minQual)
                    {
                        minQual = qt;
                    }
                    if (_param.NormalCheck)
                    {
                        ndiff = Vector3.Dot(MeshUtil.FaceNormal(vfi.F), vfi.F.FaceNormal);
                        if (ndiff < minCos)
                        {
                            minCos = ndiff;
                        }
                    }
                }
            }
            vfi = new VFIterator(_pair.V1);
            while (vfi.MoveNext())
            {
                if (vfi.F.V(0) != v0 && vfi.F.V(1) != v0 && vfi.F.V(2) != v0)
                {
                    qt = vfi.F.GetQuality();
                    if (qt < minQual)
                    {
                        minQual = qt;
                    }
                    if (_param.NormalCheck)
                    {
                        ndiff = Vector3.Dot(MeshUtil.FaceNormal(vfi.F), vfi.F.FaceNormal);
                        if (ndiff < minCos)
                        {
                            minCos = ndiff;
                        }
                    }
                }
            }

            if (minQual > _param.QualityThr) minQual = _param.QualityThr;
            if (quadErr < _param.QuadricEpsilon) quadErr = _param.QuadricEpsilon;

            _priority = (float)(quadErr / minQual);

            if (_param.NormalCheck)
            {
                if (minCos < _param.NormalCosineThr)
                {
                    _priority *= 1000;
                }
            }

            v0.Pos = oldPos0;
            v1.Pos = oldPos1;
            return _priority;
        }

        private void ComputeMinimalWithGeoContraints(
            Vector<double> vv,
            Vector<double> v0,
            Vector<double> v1,
            Quadric qsum,
            Vector<double> geo)
        {
            double min = double.MaxValue;

            if (_param.OptimalPlacement)
            {
                bool rt = qsum.MinimumWithGeoContraints(vv, geo);
                if (rt)
                {
                    return;
                }
            }

            var v = Vector<double>.Build.Dense(vv.Count);
            v[0] = geo[0]; v[1] = geo[1]; v[2] = geo[2];

            double step = _param.OptimalPlacement ? (double)1 / (_param.OptimalSampleCount + 1) : 1;

            for (double t = 0; t <= 1; t += step)
            {
                for (int i = 3; i < v.Count; i++)
                {
                    v[i] = t * v1[i] + (1 - t) * v0[i];
                }

                double q = qsum.Apply(v);
                if (q < min)
                {
                    min = q;
                    v.CopyTo(vv);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Priority()
        {
            return _priority;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFeasible()
        {
            return _sharedData.LinkConditions(_pair);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUpToDate()
        {
            var v0 = _pair.V0;
            var v1 = _pair.V1;
            return !v0.IsDeleted && !v1.IsDeleted && _locakMark >= v0.IMark && _locakMark >= v1.IMark;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MatchVertexID(Face f, Vertex v)
        {
            if (f.V(0) == v) return 0;
            if (f.V(1) == v) return 1;
            if (f.V(2) == v) return 2;
            return -1;
        }

        private static int CountDifference(
            List<(Vector<float>, Quadric)>[] lst,
            PropertySetting setting,
            Func<Vector<float>, Vector4> getV)
        {
            int size = 0;
            dynamic set;
            if (setting.SampleFunc != null)
            {
                set = new Dictionary<Vector4, List<Vector4>>();
            }
            else
            {
                set = new HashSet<Vector4>();
            }
            for (int m = 0; m < lst.Length; m++)
            {
                set.Clear();
                for (int j = 0; j < lst[m].Count; j++)
                {
                    var value = getV(lst[m][j].Item1);
                    if (setting.SampleFunc != null)
                    {
                        var sampleV = setting.SampleFunc(value);
                        if (set.TryGetValue(sampleV, out List<Vector4> values))
                        {
                            int k;
                            for (k = 0; k < values.Count; k++)
                            {
                                if (Vector4.DistanceSquared(value, values[k]) < setting.SqrDistanceThreshold)
                                {
                                    break;
                                }
                            }
                            if (k == values.Count)
                            {
                                values.Add(value);
                            }
                        }
                        else
                        {
                            set[sampleV] = new List<Vector4>() { value };
                        }
                    }
                    else
                    {
                        set.Add(value);
                    }
                }
                if (setting.SampleFunc != null)
                {
                    foreach (var values in set)
                    {
                        size += values.Value.Count;
                    }
                }
                else
                {
                    size += set.Count;
                }
            }
            return size;
        }
    }
}
