using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace COMP302.MousaHussein
{

    public struct HalfEdgeCu
    {
        public uint vertex;
        public uint triangle;
        public int opposite;
        public uint next;
        public uint cost;
    };

    struct TriangleCu
    {
        public Vector3Int tri;
        public uint halfEdge;
    };
}
