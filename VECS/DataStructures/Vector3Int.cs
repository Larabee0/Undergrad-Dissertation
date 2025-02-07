using System.Runtime.InteropServices;

namespace System.Numerics
{
    [StructLayout(LayoutKind.Sequential,Size =12)]
    public struct Vector3Int
    {
        public int X;
        public int Y;
        public int Z;

        public readonly int this[int i] => i switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _=> -1,
        };

        public Vector3Int()
        {

        }

        public Vector3Int(int v)
        {
            X = v;
            Y = v;
            Z = v;
        }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly bool Contains(int v)
        {
            return X == v || Y == v || Z == v;
        }
    }
}