using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;
using Vector3 = System.Numerics.Vector3;
namespace COMP302.Decimator
{
    public class Quadric
    {

        public Matrix<double> A;
        public Vector<double> B;
        public double C;

        public Quadric(Quadric src)
        {
            A = Matrix<double>.Build.Dense(src.A.RowCount, src.A.ColumnCount);
            src.A.CopyTo(A);
            B = Vector<double>.Build.Dense(src.B.Count);
            src.B.CopyTo(B);
            C = src.C;
        }

        public Quadric(int size)
        {
            A = Matrix<double>.Build.Dense(size, size);
            B = Vector<double>.Build.Dense(size);
            C = -1;
        }

        public int Size => B.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsValid() { return C >= 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetInvalid() { C = -1; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Quadric dst)
        {
            A.CopyTo(dst.A);
            B.CopyTo(dst.B);
            dst.C = C;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Zero()
        {
            A.Clear();
            B.Clear();
            C = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddtoQ3(Quadric q3)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    q3.A[i, j] += A[i, j];
                }
            }

            q3.B[0] += B[0];
            q3.B[1] += B[1];
            q3.B[2] += B[2];

            q3.C += C;
        }

        public void ByFace(Face face, Quadric q1, Quadric q2, Quadric q3, bool qualityQuadric, double borderWeight, VertexProperty property)
        {
            float q = face.GetQuality();
            if (q > 0)
            {
                ByFace(face, true, property);
                AddtoQ3(q1);
                AddtoQ3(q2);
                AddtoQ3(q3);
                ByFace(face, false, property);
                for (int i = 0; i < Face.VERTEX_COUNT; i++)
                {
                    if (face.IsBorder(i) || qualityQuadric)
                    {
                        Quadric temp = new(Size);
                        Vector3 newPos = (face.P0(i) + face.P1(i)) / 2 + face.FaceNormal * Vector3.Distance(face.P0(i), face.P1(i));
                        Vector<float> newAttr = (face.GetPropertyS(property, (i + 0) % 3) + face.GetPropertyS(property, (i + 1) % 3)) / 2;
                        Vector3 oldPos = face.P2(i);
                        Vector<float> oldAttr = face.GetPropertyS(property, (i + 2) % 3);

                        face.V2(i).Pos = newPos;
                        face.SetPropertyS(property, (i + 2) % 3, newAttr);

                        temp.ByFace(face, false, property);
                        temp.Scale(face.IsBorder(i) ? borderWeight : 0.05);
                        Add(temp);

                        face.V2(i).Pos = oldPos;
                        face.SetPropertyS(property, (i + 2) % 3, oldAttr);
                    }
                }
            }
            else
            {
                var attr0 = face.GetPropertyS(property, 0);
                var attr1 = face.GetPropertyS(property, 1);
                var attr2 = face.GetPropertyS(property, 2);

                var a = (attr0 - attr1).L2Norm();
                var b = (attr1 - attr2).L2Norm();
                var c = (attr2 - attr0).L2Norm();

                if (!(a + b == c || a + c == b || b + c == a))
                {
                    ByFace(face, false, property);
                }
                else
                {
                    Zero();
                }
            }
        }

        public void ByFace(Face face, bool onlyGeo, VertexProperty property)
        {
            property |= VertexProperty.Position;
            var p = face.GetPropertyD(property, 0);
            var q = face.GetPropertyD(property, 1);
            var r = face.GetPropertyD(property, 2);

            if (onlyGeo)
            {
                for (int i = 3; i < Size; i++)
                {
                    p[i] = 0;
                    q[i] = 0;
                    r[i] = 0;
                }
            }

            Vector<double> e1 = Vector<double>.Build.Dense(Size);
            Vector<double> e2 = Vector<double>.Build.Dense(Size);
            ComputeE1E2(p, q, r, e1, e2);
            ComputeQuadricFromE1E2(e1, e2, p);

            if (IsValid())
            {
                return;
            }

            double minerror = double.MaxValue;
            int minerrorIndex = 0;
            Vector<double> tmp;
            for (int i = 0; i < 7; i++)
            {
                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                    case 3:
                    case 5:
                        tmp = q;
                        q = r;
                        r = tmp;
                        break;
                    case 2:
                    case 4:
                        tmp = p;
                        p = r;
                        r = tmp;
                        break;
                    case 6: // every swap has loss of precision
                        tmp = p;
                        p = r;
                        r = tmp;
                        for (int j = 0; j <= minerrorIndex; j++)
                        {
                            switch (j)
                            {
                                case 0:
                                    break;
                                case 1:
                                case 3:
                                case 5:
                                    tmp = q;
                                    q = r;
                                    r = tmp;
                                    break;
                                case 2:
                                case 4:
                                    tmp = p;
                                    p = r;
                                    r = tmp;
                                    break;
                            }
                        }
                        minerrorIndex = -1;
                        break;
                }

                ComputeE1E2(p, q, r, e1, e2);
                ComputeQuadricFromE1E2(e1, e2, p);

                if (IsValid())
                {
                    return;
                }
                else if (minerrorIndex == -1)
                {
                    break;
                }
                else if (-C < minerror)
                {
                    minerror = -C;
                    minerrorIndex = i;
                }
            }
            C = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ComputeE1E2(Vector<double> p, Vector<double> q, Vector<double> r, Vector<double> e1, Vector<double> e2)
        {
            (q - p).Normalize(2).CopyTo(e1);

            var diffe = r - p;
            (diffe - (e1.OuterProduct(diffe) * e1)).Normalize(2).CopyTo(e2);
        }

        private void ComputeQuadricFromE1E2(Vector<double> e1, Vector<double> e2, Vector<double> p)
        {
            A = Matrix<double>.Build.DenseIdentity(Size);

            var t1 = e1.OuterProduct(e1);
            A -= t1;
            var t2 = e2.OuterProduct(e2);
            A -= t2;

            var pe1 = p.DotProduct(e1);
            var pe2 = p.DotProduct(e2);

            for (int i = 0; i < B.Count; i++)
            {
                B[i] = pe1 * e1[i] + pe2 * e2[i];
            }
            B -= p;

            C = p.DotProduct(p) - pe1 * pe1 - pe2 * pe2;
        }

        public bool MinimumWithGeoContraints(Vector<double> x, Vector<double> geo)
        {
            Matrix<double> m = A.SubMatrix(3, Size - 3, 3, Size - 3);
            Vector<double> r = Vector<double>.Build.Dense(Size - 3);

            for (int i = 0; i < r.Count; i++)
            {
                r[i] = B[i + 3];
                for (int j = 0; j < 3; j++)
                {
                    r[i] += A[i + 3, j] * geo[j];
                }
            }

            x[0] = geo[0];
            x[1] = geo[1];
            x[2] = geo[2];

            if (m.Determinant() != 0)
            {
                var result = -r * m.Inverse();
                for (int i = 0; i < result.Count; i++)
                {
                    x[i + 3] = result[i];
                }
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Minimum(Vector<double> x)
        {
            if (A.Determinant() != 0)
            {
                (-B * A.Inverse()).CopyTo(x);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Quadric q)
        {
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    A[i, j] += q.A[i, j];
                }
            }
            for (int i = 0; i < B.Count; i++)
            {
                B[i] += q.B[i];
            }
            C += q.C;
        }

        public void Sum3(Quadric q3, Vector<float> props)
        {
            if (q3.Size != 3)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    A[i, j] += q3.A[i, j];
                }
            }
            for (int i = 3; i < Size; i++)
            {
                A[i, i] += 1;
            }

            B[0] += q3.B[0];
            B[1] += q3.B[1];
            B[2] += q3.B[2];
            for (int i = 3; i < Size; i++)
            {
                B[i] -= props[i - 3];
            }

            C += q3.C + props.DotProduct(props);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(double val)
        {
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    A[i, j] *= val;
                }
            }
            for (int i = 0; i < B.Count; i++)
            {
                B[i] *= val;
            }
            C *= val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Apply(Vector<double> v)
        {
            return (A * v).DotProduct(v) + 2 * B.DotProduct(v) + C;
        }
    }
}
