using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
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

        public Vertex[] vertices { get; private set; } = new Vertex[VERTEX_COUNT];
        public Vector3[] normals { get; private set; } = new Vector3[VERTEX_COUNT];
        public Vector2[] uvs { get; private set; } = new Vector2[VERTEX_COUNT];
        
        public int subMeshIndex { get; set; }
        public Vector3 faceNormal { get; private set; }

        public Face[] vfParent { get; private set; } = new Face[VERTEX_COUNT];
        public int[] vfIndex { get; private set; } = new int[VERTEX_COUNT];

        #region Vertex
        public ref Vertex V(int index) => ref this.vertices[index];
        public Vector3 P(int index) => this.vertices[index].pos;

        public Vertex V0(int index) => this.V(index);
        public Vertex V1(int index) => this.V((index + 1) % VERTEX_COUNT);
        public Vertex V2(int index) => this.V((index + 2) % VERTEX_COUNT);

        public Vector3 P0(int index) => this.V(index).pos;
        public Vector3 P1(int index) => this.V((index + 1) % VERTEX_COUNT).pos;
        public Vector3 P2(int index) => this.V((index + 2) % VERTEX_COUNT).pos;
        #endregion

        #region Property
        public static int GetPropertySize(VertexProperty property, Mesh mesh)
        {
            int count = 0;
            if (property.HasFlag(VertexProperty.Position))
            {
                count += 3;
            }
            if (property.HasFlag(VertexProperty.Normal))
            {
                count += 3;
            }
            if (property.HasFlag(VertexProperty.UV0))
            {
                count += 2;
            }
            return count;
        }

        public Vector<float> GetPropertyS(VertexProperty property, int id)
        {
            List<float> result = new List<float>();
            this.InternalGetProperty(property, id, result);
            return Vector<float>.Build.Dense(result.ToArray());
        }

        public Vector<double> GetPropertyD(VertexProperty property, int id)
        {
            List<double> result = new List<double>();
            this.InternalGetProperty(property, id, result);
            return Vector<double>.Build.Dense(result.ToArray());
        }

        public void SetPropertyS(VertexProperty property, int id, Vector<float> value)
        {
            this.InternalSetProperty(property, id, value);
        }

        public void SetPropertyD(VertexProperty property, int id, Vector<double> value)
        {
            this.InternalSetProperty(property, id, value);
        }

        private void InternalGetProperty(VertexProperty property, int id, dynamic result)
        {
            if (property.HasFlag(VertexProperty.Position))
            {
                var pos = this.P(id);
                result.Add(pos.X);
                result.Add(pos.Y);
                result.Add(pos.Z);
            }
            if (property.HasFlag(VertexProperty.Normal))
            {
                var normal = this.normals[id];
                result.Add(normal.X);
                result.Add(normal.Y);
                result.Add(normal.Z);
            }
            if (property.HasFlag(VertexProperty.UV0))
            {
                var uv = this.uvs[id];
                result.Add(uv.X);
                result.Add(uv.Y);
            }
        }

        private void InternalSetProperty(VertexProperty property, int id, dynamic value)
        {
            int index = 0;
            if (property.HasFlag(VertexProperty.Position))
            {
                this.V(id).pos = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
            }
            if (property.HasFlag(VertexProperty.Normal))
            {
                this.normals[id] = new Vector3((float)value[index++], (float)value[index++], (float)value[index++]);
            }
            if (property.HasFlag(VertexProperty.UV0))
            {
                this.uvs[id] = new Vector2((float)value[index++], (float)value[index++]);
            }
        }
        #endregion

        #region Interploation
        public Vector3 InterpolateNormal(Vector3 barycentric)
        {
            return Vector3.Normalize(this.normals[0] * barycentric.X + this.normals[1] * barycentric.Y + this.normals[2] * barycentric.Z);
        }

        public Vector2 InterpolateUV(Vector3 barycentric)
        {
            return this.uvs[0] * barycentric.X + this.uvs[1] * barycentric.Y + this.uvs[2] * barycentric.Z;
        }

        #endregion

        #region Flags
        public bool IsDeleted() { return this.HasFlag((int)FaceFlags.Deleted); }
        public void SetDeleted() { this.AddFlag((int)FaceFlags.Deleted); }
        public void ClearDeleted() { this.RemoveFlag((int)FaceFlags.Deleted); }

        public bool IsVisited() { return this.HasFlag((int)FaceFlags.Visited); }
        public void SetVisited() { this.AddFlag((int)FaceFlags.Visited); }
        public void ClearVisited() { this.RemoveFlag((int)FaceFlags.Deleted); }

        public bool IsWritable() => !this.HasFlag((int)FaceFlags.NotWrite);
        public void SetWritable() => this.RemoveFlag((int)FaceFlags.NotWrite);
        public void ClearWritable() => this.AddFlag((int)FaceFlags.NotWrite);

        public bool IsBorder(int index) => this.HasFlag((int)FaceFlags.Border0 << index);
        public void SetBorder(int index) => this.AddFlag((int)FaceFlags.Border0 << index);
        public void ClearBorder(int index) => this.RemoveFlag((int)FaceFlags.Border0 << index);

        public void ClearBorderFlags()
        {
            this.RemoveFlag((int)(FaceFlags.Border0 | FaceFlags.Border1 | FaceFlags.Border2));
        }
        #endregion

        public void BuildFaceNormal()
        {
            this.faceNormal = MeshUtil.FaceNormal(this);
        }

        public float GetQuality()
        {
            var p0 = this.P(0);
            var p1 = this.P(1);
            var p2 = this.P(2);
            Vector3 d10 = p1 - p0;
            Vector3 d20 = p2 - p0;
            Vector3 d12 = p1 - p2;
            Vector3 X = Vector3.Cross(d10, d20);

            float a = X.Length();
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
            if (f.V(Z).vfParent == f)
            {
                int fz = f.V(Z).vfIndex;
                f.V(Z).vfParent = f.vfParent[fz];
                f.V(Z).vfIndex = f.vfIndex[fz];
            }
            else
            {
                VFIterator vfi = new VFIterator(f.V(Z));
                Face tf;
                int tz;
                vfi.MoveNext();
                while (true)
                {
                    tf = vfi.f;
                    tz = vfi.z;
                    if (!vfi.MoveNext())
                    {
                        break;
                    }
                    if (vfi.f == f)
                    {
                        tf.vfParent[tz] = f.vfParent[Z];
                        tf.vfIndex[tz] = f.vfIndex[Z];
                        break;
                    }
                }
            }
        }
    }
}
