using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using VECS;

namespace COMP302.Decimator
{
    public static class MeshUtil
    {
        public static List<Face> DoCollapse(Mesh m, VertexPair c, Vector3 p)
        {
            var deleted = new List<Face>();
            var av01 = new List<(Face, int)>();
            var av0 = new List<(Face, int)>();

            VFIterator vfi = new(c.V0);
            while (vfi.MoveNext())
            {
                bool foundV1 = false;
                for (int i = 0; i < Face.VERTEX_COUNT; i++)
                {
                    if (vfi.F.V(i) == c.V1)
                    {
                        foundV1 = true;
                        break;
                    }
                }
                if (foundV1) av01.Add((vfi.F, vfi.Z));
                else av0.Add((vfi.F, vfi.Z));
            }

            for (int i = 0; i < av01.Count; i++)
            {
                var face = av01[i].Item1;
                Face.VFDetach(face, (av01[i].Item2 + 1) % Face.VERTEX_COUNT);
                Face.VFDetach(face, (av01[i].Item2 + 2) % Face.VERTEX_COUNT);
                Mesh.DeleteFace(m, face);
                deleted.Add(face);
            }

            for (int i = 0; i < av0.Count; i++)
            {
                var face = av0[i].Item1;
                var Z = av0[i].Item2;

                face.V(Z) = c.V1;
                face.VfParent[Z] = c.V1.VfParent;
                face.VfIndex[Z] = c.V1.VfIndex;
                c.V1.VfParent = face;
                c.V1.VfIndex = Z;
            }

            Mesh.DeleteVertex(m, c.V0);
            c.V1.Pos = p;
            return deleted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FaceNormal(Face face)
        {
            var e1 = face.P(1) - face.P(0);
            var e2 = face.P(2) - face.P(0);
            var n = Vector3.Cross(e1, e2);
            return Vector3.Normalize(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 BarycentricCoords(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = (b - a), v1 = (c - a), v2 = (point - a);
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float W = (d00 * d21 - d01 * d20) / denom;
            float u = 1f - v - W;
            return new Vector3(u, v, W);
        }

        public static (Vector3 closest, Vector3 barycentric, float sqrDistance) PointToTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            Vector3 diff = p - a;
            Vector3 edge0 = b - a;
            Vector3 edge1 = c - a;
            float a00 = Vector3.Dot(edge0, edge0);
            float a01 = Vector3.Dot(edge0, edge1);
            float a11 = Vector3.Dot(edge1, edge1);
            float b0 = -Vector3.Dot(diff, edge0);
            float b1 = -Vector3.Dot(diff, edge1);
            float det = a00 * a11 - a01 * a01;
            float t0 = a01 * b1 - a11 * b0;
            float t1 = a01 * b0 - a00 * b1;

            if (t0 + t1 <= det)
            {
                if (t0 < 0)
                {
                    if (t1 < 0)
                    {
                        if (b0 < 0)
                        {
                            t1 = 0;
                            if (-b0 >= a00)
                            {
                                t0 = 1;
                            }
                            else
                            {
                                t0 = -b0 / a00;
                            }
                        }
                        else
                        {
                            t0 = 0;
                            if (b1 >= 0)
                            {
                                t1 = 0;
                            }
                            else if (-b1 >= a11)
                            {
                                t1 = 1;
                            }
                            else
                            {
                                t1 = -b1 / a11;
                            }
                        }
                    }
                    else
                    {
                        t0 = 0;
                        if (b1 >= 0)
                        {
                            t1 = 0;
                        }
                        else if (-b1 >= a11)
                        {
                            t1 = 1;
                        }
                        else
                        {
                            t1 = -b1 / a11;
                        }
                    }
                }
                else if (t1 < 0)
                {
                    t1 = 0;
                    if (b0 >= 0)
                    {
                        t0 = 0;
                    }
                    else if (-b0 >= a00)
                    {
                        t0 = 1;
                    }
                    else
                    {
                        t0 = -b0 / a00;
                    }
                }
                else
                {
                    float invDet = 1 / det;
                    t0 *= invDet;
                    t1 *= invDet;
                }
            }
            else
            {
                float tmp0, tmp1, numer, denom;

                if (t0 < 0)
                {
                    tmp0 = a01 + b0;
                    tmp1 = a11 + b1;
                    if (tmp1 > tmp0)
                    {
                        numer = tmp1 - tmp0;
                        denom = a00 - 2 * a01 + a11;
                        if (numer >= denom)
                        {
                            t0 = 1;
                            t1 = 0;
                        }
                        else
                        {
                            t0 = numer / denom;
                            t1 = 1 - t0;
                        }
                    }
                    else
                    {
                        t0 = 0;
                        if (tmp1 <= 0)
                        {
                            t1 = 1;
                        }
                        else if (b1 >= 0)
                        {
                            t1 = 0;
                        }
                        else
                        {
                            t1 = -b1 / a11;
                        }
                    }
                }
                else if (t1 < 0)
                {
                    tmp0 = a01 + b1;
                    tmp1 = a00 + b0;
                    if (tmp1 > tmp0)
                    {
                        numer = tmp1 - tmp0;
                        denom = a00 - 2 * a01 + a11;
                        if (numer >= denom)
                        {
                            t1 = 1;
                            t0 = 0;
                        }
                        else
                        {
                            t1 = numer / denom;
                            t0 = 1 - t1;
                        }
                    }
                    else
                    {
                        t1 = 0;
                        if (tmp1 <= 0)
                        {
                            t0 = 1;
                        }
                        else if (b0 >= 0)
                        {
                            t0 = 0;
                        }
                        else
                        {
                            t0 = -b0 / a00;
                        }
                    }
                }
                else
                {
                    numer = a11 + b1 - a01 - b0;
                    if (numer <= 0)
                    {
                        t0 = 0;
                        t1 = 1;
                    }
                    else
                    {
                        denom = a00 - 2 * a01 + a11;
                        if (numer >= denom)
                        {
                            t0 = 1;
                            t1 = 0;
                        }
                        else
                        {
                            t0 = numer / denom;
                            t1 = 1 - t0;
                        }
                    }
                }
            }
            var closest = a + t0 * edge0 + t1 * edge1;
            diff = p - closest;
            return (closest, new Vector3(1 - t0 - t1, t0, t1), diff.LengthSquared());
        }

        public static bool IsLineIntersectTriangle(Vector3 q1, Vector3 q2, Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 result)
        {
            static float SignedTetraVolume(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var v = Vector3.Dot(Vector3.Cross(b - a, c - a), d - a) / 6f;
                return v > 0 ? 1f : v < 0 ? -1f : 0f;
            }

            var s1 = SignedTetraVolume(q1, p1, p2, p3);
            var s2 = SignedTetraVolume(q2, p1, p2, p3);

            if (s1 != s2)
            {
                var s3 = SignedTetraVolume(q1, q2, p1, p2);
                var s4 = SignedTetraVolume(q1, q2, p2, p3);
                var s5 = SignedTetraVolume(q1, q2, p3, p1);

                if (s3 == s4 && s4 == s5)
                {
                    var n = Vector3.Cross(p2 - p1, p3 - p1);
                    var m = Vector3.Dot(q2 - q1, n);
                    if (m != 0)
                    {
                        var t = Vector3.Dot(p1 - q1, n) / m;
                        result = q1 + t * (q2 - q1);
                        return true;
                    }
                }
            }
            result = Vector3.Zero;
            return false;
        }

        public static bool IsLineInBox(Vector3 a, Vector3 b, Bounds bounds)
        {
            a -= bounds.center;
            b -= bounds.center;

            // Get line midpoint and extent
            Vector3 mid = (a + b) * 0.5f;
            Vector3 l = (a - mid);
            Vector3 ext = new(MathF.Abs(l.X), MathF.Abs(l.Y), MathF.Abs(l.Z));

            // Use Separating Axis Test
            // Separation vector from box center to line center is LMid, since the line is in box space
            if (MathF.Abs(mid.X) > bounds.extents.X + ext.X) return false;
            if (MathF.Abs(mid.Y) > bounds.extents.Y + ext.Y) return false;
            if (MathF.Abs(mid.Z) > bounds.extents.Z + ext.Z) return false;
            // Crossproducts of line and each axis
            if (MathF.Abs(mid.Y * l.Z - mid.Z * l.Y) > (bounds.extents.Y * ext.Z + bounds.extents.Z * ext.Y)) return false;
            if (MathF.Abs(mid.X * l.Z - mid.Z * l.X) > (bounds.extents.X * ext.Z + bounds.extents.Z * ext.X)) return false;
            if (MathF.Abs(mid.X * l.Y - mid.Y * l.X) > (bounds.extents.X * ext.Y + bounds.extents.Y * ext.X)) return false;
            // No separating axis, the line intersects
            return true;
        }
    }
}
