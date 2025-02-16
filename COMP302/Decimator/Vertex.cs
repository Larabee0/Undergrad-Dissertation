using System;
using System.Numerics;

namespace COMP302.Decimator
{
    public class VertexPair
    {
        public Vertex V0 { get; private set; }
        public Vertex V1 { get; private set; }

        public VertexPair(Vertex v0, Vertex v1)
        {
            V0 = v0;
            V1 = v1;
        }
    }

    public class Vertex : FlagBase
    {

        [Flags]
        public enum VertexFlags : int
        {
            Deleted = 1 << 0,
            NotRead = 1 << 1,
            NotWrite = 1 << 2,
            Modified = 1 << 3,
            Visited = 1 << 4,
            Border = 1 << 5,
            User = 1 << 6,
        }

        public Vector3 Pos;
        public Face VfParent;
        public int VfIndex;
        public int IMark = 0;

        public bool IsDeleted => HasFlag((int)VertexFlags.Deleted);
        public bool IsVisited => HasFlag((int)VertexFlags.Visited);
        public bool IsWritable => !HasFlag((int)VertexFlags.NotWrite);

        public Vertex(Vector3 pos)
        {
            Pos = pos;
        }

        public void InitIMark()
        {
            IMark = 0;
        }

        #region Flags
        public void SetDeleted() => AddFlag((int)VertexFlags.Deleted);

        public void SetVisited() => AddFlag((int)VertexFlags.Visited);
        public void ClearVisited() => RemoveFlag((int)VertexFlags.Visited);

        public void ClearWritable() => AddFlag((int)VertexFlags.NotWrite);
        #endregion

        #region Operators
        public static bool operator <(Vertex X, Vertex Y)
        {
            return (X.Pos.Z != Y.Pos.Z) ? (X.Pos.Z < Y.Pos.Z) : (X.Pos.Y != Y.Pos.Y) ? (X.Pos.Y < Y.Pos.Y) : (X.Pos.X < Y.Pos.X);
        }

        public static bool operator >(Vertex X, Vertex Y)
        {
            return (X.Pos.Z != Y.Pos.Z) ? (X.Pos.Z > Y.Pos.Z) : (X.Pos.Y != Y.Pos.Y) ? (X.Pos.Y > Y.Pos.Y) : (X.Pos.X > Y.Pos.X);
        }

        public static bool operator <=(Vertex X, Vertex Y)
        {
            return (X.Pos.Z != Y.Pos.Z) ? (X.Pos.Z < Y.Pos.Z) : (X.Pos.Y != Y.Pos.Y) ? (X.Pos.Y < Y.Pos.Y) : (X.Pos.X <= Y.Pos.X);
        }

        public static bool operator >=(Vertex X, Vertex Y)
        {
            return (X.Pos.Z != Y.Pos.Z) ? (X.Pos.Z > Y.Pos.Z) : (X.Pos.Y != Y.Pos.Y) ? (X.Pos.Y > Y.Pos.Y) : (X.Pos.X >= Y.Pos.X);
        }
        #endregion
    }
}
