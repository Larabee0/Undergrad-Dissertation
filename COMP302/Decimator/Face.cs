using MathNet.Numerics.LinearAlgebra;
using System;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace COMP302.Decimator
{
    public class Face : FlagBase
    {

        public const int VERTEX_COUNT = 3;

        [Flags]
        public enum FaceFlags : int
        {
            Deleted = 1 << 0,
            NotRead = 1 << 1,
            NotWrite = 1 << 2,
            Visited = 1 << 3,
            Border0 = 1 << 4,
            Border1 = 1 << 5,
            Border2 = 1 << 6,
        }

        public Vertex[] Vertices { get; private set; } = new Vertex[VERTEX_COUNT];
        public Vector3[] Normals { get; private set; } = new Vector3[VERTEX_COUNT];
        public Vector2[] Uvs { get; private set; } = new Vector2[VERTEX_COUNT];
        
        public int SubMeshIndex { get; set; }
        public Vector3 FaceNormal { get; private set; }

        public Face[] VfParent { get; private set; } = new Face[VERTEX_COUNT];
        public int[] VfIndex { get; private set; } = new int[VERTEX_COUNT];

        #region Vertex
        public ref Vertex V(int index) => ref Vertices[index];
        public Vector3 P(int index) => Vertices[index].Pos;

        public Vertex V0(int index) => V(index);
        public Vertex V1(int index) => V((index + 1) % VERTEX_COUNT);
        public Vertex V2(int index) => V((index + 2) % VERTEX_COUNT);

        public Vector3 P0(int index) => V(index).Pos;
        public Vector3 P1(int index) => V((index + 1) % VERTEX_COUNT).Pos;
        public Vector3 P2(int index) => V((index + 2) % VERTEX_COUNT).Pos;
        #endregion

        #region Property
        public static int GetPropertySize(VertexProperty property)
        {
            int count = 0;
            if ((property & VertexProperty.Position) != 0)
            {
                count += 3;
            }
            if ((property & VertexProperty.Normal) != 0)
            {
                count += 3;
            }
            if ((property & VertexProperty.UV0) != 0)
            {
                count += 2;
            }
            return count;
        }

        public Vector<float> GetPropertyS(VertexProperty property, int id)
        {
            return Vector<float>.Build.Dense(InternalGetProperty(property, id));
        }

        public Vector<double> GetPropertyD(VertexProperty property, int id)
        {
            return Vector<double>.Build.Dense(InternalGetPropertyD(property, id));
        }

        public void SetPropertyS(VertexProperty property, int id, Vector<float> value)
        {
            InternalSetProperty(property, id, value);
        }

        public void SetPropertyD(VertexProperty property, int id, Vector<double> value)
        {
            InternalSetProperty(property, id, value);
        }

        private float[] InternalGetProperty(VertexProperty property, int id)
        {
            float[] rPos = [];
            float[] rNorm = [];
            float[] rUv = [];
            if ((property & VertexProperty.Position) != 0)
            {
                var pos = P(id);
                rPos = [pos.X, pos.Y, pos.Z];
            }
            if ((property & VertexProperty.Normal)!=0)
            {
                var normal = Normals[id];
                rNorm = [normal.X, normal.Y, normal.Z];
            }
            if ((property & VertexProperty.UV0) != 0)
            {
                var uv = Uvs[id];
                rUv = [uv.X, uv.Y];
            }
            return [.. rPos, .. rNorm, .. rUv];
        }

        private double[] InternalGetPropertyD(VertexProperty property, int id)
        {
            double[] rPos = [];
            double[] rNorm = [];
            double[] rUv = [];
            if ((property & VertexProperty.Position) != 0)
            {
                var pos = P(id);
                rPos = [pos.X, pos.Y, pos.Z];
            }
            if ((property & VertexProperty.Normal) != 0)
            {
                var normal = Normals[id];
                rNorm = [normal.X, normal.Y, normal.Z];
            }
            if ((property & VertexProperty.UV0) != 0)
            {
                var uv = Uvs[id];
                rUv = [uv.X, uv.Y];
            }
            return [.. rPos, .. rNorm, .. rUv];
        }

        private void InternalSetProperty(VertexProperty property, int id, dynamic value)
        {
            int index = 0;
            if ((property & VertexProperty.Position) != 0)
            {
                V(id).Pos = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
            }
            if ((property & VertexProperty.Normal) != 0)
            {
                Normals[id] = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
            }
            if ((property & VertexProperty.UV0) != 0)
            {
                Uvs[id] = new Vector2((float)value[index++], (float)value[index++]);
            }
        }
        #endregion

        #region Interploation
        public Vector3 InterpolateNormal(Vector3 barycentric)
        {
            return Vector3.Normalize(Normals[0] * barycentric.X + Normals[1] * barycentric.Y + Normals[2] * barycentric.Z);
        }

        public Vector2 InterpolateUV(Vector3 barycentric)
        {
            return Uvs[0] * barycentric.X + Uvs[1] * barycentric.Y + Uvs[2] * barycentric.Z;
        }

        #endregion

        #region Flags
        public bool IsDeleted() { return HasFlag((int)FaceFlags.Deleted); }
        public void SetDeleted() { AddFlag((int)FaceFlags.Deleted); }
        public void ClearDeleted() { RemoveFlag((int)FaceFlags.Deleted); }

        public bool IsVisited() { return HasFlag((int)FaceFlags.Visited); }
        public void SetVisited() { AddFlag((int)FaceFlags.Visited); }
        public void ClearVisited() { RemoveFlag((int)FaceFlags.Deleted); }

        public bool IsWritable() => !HasFlag((int)FaceFlags.NotWrite);
        public void SetWritable() => RemoveFlag((int)FaceFlags.NotWrite);
        public void ClearWritable() => AddFlag((int)FaceFlags.NotWrite);

        public bool IsBorder(int index) => HasFlag((int)FaceFlags.Border0 << index);
        public void SetBorder(int index) => AddFlag((int)FaceFlags.Border0 << index);
        public void ClearBorder(int index) => RemoveFlag((int)FaceFlags.Border0 << index);

        public void ClearBorderFlags()
        {
            RemoveFlag((int)(FaceFlags.Border0 | FaceFlags.Border1 | FaceFlags.Border2));
        }
        #endregion

        public void BuildFaceNormal()
        {
            FaceNormal = MeshUtil.FaceNormal(this);
        }

        public float GetQuality()
        {
            var p0 = P(0);
            var p1 = P(1);
            var p2 = P(2);
            Vector3 d10 = p1 - p0;
            Vector3 d20 = p2 - p0;
            Vector3 d12 = p1 - p2;

            float a = Vector3.Cross(d10, d20).Length();
            if (a == 0) return 0;
            float b = d10.LengthSquared();
            if (b == 0) return 0;
            float t;
            t = d20.LengthSquared();
            if (b < t) b = t;
            t = d12.LengthSquared();
            if (b < t) b = t;
            return a / b;
        }

        public static void VFDetach(Face f, int Z)
        {
            if (f.V(Z).VfParent == f)
            {
                int fz = f.V(Z).VfIndex;
                f.V(Z).VfParent = f.VfParent[fz];
                f.V(Z).VfIndex = f.VfIndex[fz];
            }
            else
            {
                VFIterator vfi = new(f.V(Z));
                Face tf;
                int tz;
                vfi.MoveNext();
                while (true)
                {
                    tf = vfi.F;
                    tz = vfi.Z;
                    if (!vfi.MoveNext())
                    {
                        break;
                    }
                    if (vfi.F == f)
                    {
                        tf.VfParent[tz] = f.VfParent[Z];
                        tf.VfIndex[tz] = f.VfIndex[Z];
                        break;
                    }
                }
            }
        }
    }
}
