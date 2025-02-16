using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

namespace COMP302.Decimator
{

    public sealed class QuadricHelper
    {

        private readonly Dictionary<Vertex, List<(Vector<float>, Quadric)>> QuadricNTemp = [];
        private readonly Dictionary<Vertex, Quadric> QuadricTemp = [];

        public QuadricHelper(IEnumerable<Vertex> verts)
        {
            foreach (var vert in verts)
            {
                var q = new Quadric(3);
                q.Zero();
                QuadricTemp[vert] = q;
                QuadricNTemp[vert] = [];
            }
        }

        public void Alloc(Vertex vert, Vector<float> props)
        {
            var qv = QuadricNTemp[vert];
            var newq = new Quadric(3 + props.Count);
            newq.Zero();
            newq.Sum3(Qd3(vert), props);
            qv.Add((props, newq));
        }

        public void SumAll(Vertex vert, Vector<float> props, Quadric q)
        {
            var qv = QuadricNTemp[vert];
            for (int i = 0; i < qv.Count; i++)
            {
                Vector<float> p = qv[i].Item1;
                if (p.Equals(props))
                {
                    qv[i].Item2.Add(q);
                }
                else
                {
                    qv[i].Item2.Sum3(Qd3(vert), p);
                }
            }
        }

        public bool Contains(Vertex vert, Vector<float> props)
        {
            var qv = QuadricNTemp[vert];
            for (int i = 0; i < qv.Count; i++)
            {
                Vector<float> p = qv[i].Item1;
                if (p.Equals(props))
                {
                    return true;
                }
            }
            return false;
        }

        public Quadric Qd(Vertex vert, Vector<float> props)
        {
            var qv = QuadricNTemp[vert];
            for (int i = 0; i < qv.Count; i++)
            {
                Vector<float> p = qv[i].Item1;
                if (p.Equals(props))
                {
                    return qv[i].Item2;
                }
            }
            return qv[0].Item2;
        }

        public Quadric Qd3(Vertex vert)
        {
            return QuadricTemp[vert];
        }

        public void Qd3(Vertex vert, Quadric value)
        {
            QuadricTemp[vert] = value;
        }

        public List<(Vector<float>, Quadric)> Vd(Vertex vert)
        {
            return QuadricNTemp[vert];
        }

        public void Vd(Vertex vert, List<(Vector<float>, Quadric)> value)
        {
            QuadricNTemp[vert] = value;
        }
    }
}
