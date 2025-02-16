using System.Collections;

namespace COMP302.Decimator
{
    public class VFIterator : IEnumerator
    {
        public Face F { get; private set; }
        public int Z { get; private set; }

        private readonly Vertex _v;
        private bool _init = false;

        public VFIterator(Vertex v)
        {
            _v = v;
        }

        public void Reset()
        {
            _init = false;
        }

        public bool MoveNext()
        {
            if (!_init)
            {
                F = _v.VfParent;
                Z = _v.VfIndex;
                _init = true;
            }
            else if (F != null)
            {
                var t = F;
                F = t.VfParent[Z];
                Z = t.VfIndex[Z];
            }
            return F != null;
        }

        public object Current => this;

        public Vertex V0 => F.V0(Z);
        public Vertex V1 => F.V1(Z);
        public Vertex V2 => F.V2(Z);
    }
}
