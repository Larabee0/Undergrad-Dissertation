using System;
using SDL_Vulkan_CS.VulkanBackend;
using System.Numerics;

namespace SDL_Vulkan_CS.Comp302
{
    /// <summary>
    /// C++ Source: https://meshdev.sourceforge.net/
    /// LICENSE GNU General Public License
    /// Translated to C# by William Vickers Hastings
    /// </summary>
    public class UniformGrid
    {
        private readonly float m_rSize;

        // Cells array
        private readonly Cell3D[][][] m_pCell;

        // Min cell coordinates
        private Vector3 m_pMin;

        // Cells number in the 3 dimensions
        private Vector3Int m_pCellNum;

        // Faces tested number
        private int _FacesTested;

        // Nearest Neighbors
        private Neighborhood neighbors;

        private readonly Vertex[] mv;
        private readonly Vector3Int[] mf;
        private readonly Vector3[] mfn;
        private readonly float[] mp; // Mesh face planes

        Cell3D pCell;

        public UniformGrid(Mesh m, Bounds bbox, float dim)
        {
            int i, j, k, l;
            mv = m.Vertices;
            mf = m.Faces;
            mfn = m.FaceNormals;
            mp = new float[mf.Length];
            for (i = 0; i < mp.Length; i++)
            {
                mp[i] = Vector3.Dot(-mfn[i], mv[mf[i][0]].Position);
            }


            m_pMin = new(bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
            m_rSize = dim;

            m_pCellNum = new((int)(bbox.Size.Length() / m_rSize + 1.0f));

            m_pCell = new Cell3D[m_pCellNum[0]][][];

            for (i = 0; i < m_pCellNum[0]; i++)
            {
                var cell = m_pCell[i] = new Cell3D[m_pCellNum[1]][];
                for (j = 0; j < m_pCellNum[1]; j++)
                {
                    cell[j] = new Cell3D[m_pCellNum[2]];
                }
            }

            for (i = 0; i < mv.Length; i++)
            {
                //////////////////////////////////////////////////////
                // Compute cell position
                j = (int)((mv[i].Position.X - m_pMin.X) / m_rSize);
                k = (int)((mv[i].Position.Y - m_pMin.Y) / m_rSize);
                l = (int)((mv[i].Position.Z - m_pMin.Z) / m_rSize);
                /////////////////////////////////////////////////////
                // Register point
                AddOnePoint(i, j, k, l);
            }

            SetFaces();

            _FacesTested = 0;
            neighbors = null;
        }

        public void SetFaces()
        {

            /////////////////////////////////////
            // Begin to work with vertices
            for (int i = 0; i < mf.Length; i++)
            {
                SetFace(i);
            }
        }

        private void SetFace(int i)
        {
            /////////////////////////////////////////
            // Set vertices index
            int a = mf[i][0];
            int b = mf[i][1];
            int c = mf[i][2];
            //////////////////////////////////////////////////////
            // Compute cell position
            int x1, x2, y1, y2, z1, z2;
            x1 = x2 = (int)((mv[a].Position.X - m_pMin.X) / m_rSize);
            y1 = y2 = (int)((mv[a].Position.Y - m_pMin.Y) / m_rSize);
            z1 = z2 = (int)((mv[a].Position.Z - m_pMin.Z) / m_rSize);
            //////////////////////////////////////////////////////
            // Compute cell position
            ComputeCellPosition(b,ref x1,ref x2, ref y1, ref y2, ref z1, ref z2);
            //////////////////////////////////////////////////////
            // Compute cell position
            ComputeCellPosition(c, ref x1, ref x2, ref y1, ref y2, ref z1, ref z2);
            /////////////////////////////////////////////////////:
            // Compute intersection Plane-Cube
            ComputerIntersectionPlaneCube(i, x1, x2, y1, y2, z1, z2);
        }

        private void ComputeCellPosition(int vertex, ref int x1, ref int x2, ref int y1, ref int y2, ref int z1, ref int z2)
        {
            int xx = (int)((mv[vertex].Position.X - m_pMin.X) / m_rSize);
            int yy = (int)((mv[vertex].Position.Y - m_pMin.Y) / m_rSize);
            int zz = (int)((mv[vertex].Position.Z - m_pMin.Z) / m_rSize);
            // Check for x
            if (xx < x1) x1 = xx;
            else if (xx > x2) x2 = xx;
            // Check for y
            if (yy < y1) y1 = yy;
            else if (yy > y2) y2 = yy;
            // Check for z
            if (zz < z1) z1 = zz;
            else if (zz > z2) z2 = zz;
        }

        private void ComputerIntersectionPlaneCube(int i, int x1, int x2, int y1, int y2, int z1, int z2)
        {
            for (int xx = x1; xx <= x2; xx++)
            {
                for (int yy = y1; yy <= y2; yy++)
                {
                    for (int zz = z1; zz <= z2; zz++)
                    {
                        ////////////////////////////////////////
                        Vector3 p = new()
                        {
                            X = m_pMin.X + xx * m_rSize,
                            Y = m_pMin.Y + yy * m_rSize,
                            Z = m_pMin.Z + zz * m_rSize
                        };
                        char s;
                        if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0) s = (char)1;
                        else s = (char)2;
                        //////////////////////////////////
                        p.X += m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.Z += m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.X -= m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.Y += m_rSize;
                        p.Z -= m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.X += m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.Z += m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        //////////////////////////////////
                        p.Y -= m_rSize;
                        if (s == 1)
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) >= 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                        else
                        {
                            if (DistancePoint2Plane(p, mfn[i], mp[i]) < 0)
                            { AddOneFace(i, xx, yy, zz); continue; }
                        }
                    }
                }
            }
        }

        private static float DistancePoint2Plane(Vector3 v, Vector3 n, float h)
        {
            return Vector3.Dot(v, n) + h;
        }

        private void AddOneFace(int n, int x, int y, int z)
        {
            //////////////////////////////////////////////////////
            // Is there an existing registered point
            if (m_pCell[x][y][z] == null)
            {
                m_pCell[x][y][z] = new Cell3D()
                {
                    v = -1,
                    f = n
                };
            }
            else
            {
                pCell = m_pCell[x][y][z];
                while ((pCell.f != -1) && (pCell.next != null))
                    pCell = pCell.next;
                if (pCell.f == -1) pCell.f = n;
                else
                {
                    pCell.next = new Cell3D()
                    {
                        v = -1,
                        f = n
                    };
                }
            }
        }
        private static int Clamp(int x, int max)
        {
            return (x < 0) ? 0 : (x >= max) ? (max - 1) : x;
        }

        public Neighborhood NearestNeighbors(Vector3 point)
        {
            float d;
            int i, j, k, n;
            int xx, yy, zz;
            int ia, ib, ja, jb, ka, kb;

            //////////////////////////////////////////////////////
            // Compute cell position
            xx = (int)((point.X - m_pMin.X) / m_rSize);
            yy = (int)((point.Y - m_pMin.Y) / m_rSize);
            zz = (int)((point.Z - m_pMin.Z) / m_rSize);

            n = 0;

            //delete neighbors;
            //neighbors = 0;
            neighbors = new Neighborhood();

            //////////////////////////////////////////////////////////
            // Check for point
            //////////////////////////////////////////////////////////
            do
            {
                n++;

                ia = Clamp(xx - n, m_pCellNum[0]);
                ib = Clamp(xx + n, m_pCellNum[0]);
                ja = Clamp(yy - n, m_pCellNum[1]);
                jb = Clamp(yy + n, m_pCellNum[1]);
                ka = Clamp(zz - n, m_pCellNum[2]);
                kb = Clamp(zz + n, m_pCellNum[2]);

                for (i = ia; i <= ib; i++)
                {
                    for (j = ja; j <= jb; j++)
                    {
                        for (k = ka; k <= kb; k++)
                        {
                            if (m_pCell[i][j][k] == null) continue;
                            pCell = m_pCell[i][j][k];
                            do
                            {
                                /////////////////////////////////////////////
                                // compute euclidian distance
                                // between current sampled point &
                                // current reference mesh point
                                if (pCell.v != -1)
                                {
                                    d = (point - mv[pCell.v].Position).Length();
                                    if (d <= neighbors.Distance())
                                    {
                                        if (d == neighbors.Distance())
                                        {
                                            neighbors.AddVertex(mv[pCell.v].Position, pCell.v);
                                        }
                                        else
                                        {
                                            neighbors.NewVertex(d, mv[pCell.v].Position, pCell.v);
                                            if (d == 0) return (neighbors);
                                        }
                                    }
                                }

                                /////////////////////////////////////////////
                                // compute euclidian distance
                                // between current sampled point &
                                // current reference mesh point
                                if (pCell.f != -1)
                                {
                                    DistancePoint2Face(point, pCell.f);
                                    if (neighbors.Distance() == 0) return (neighbors);
                                }

                                pCell = pCell.next;

                            }
                            while (pCell != null);
                        }
                    }
                }
            }
            while (neighbors.Distance() > (n * m_rSize));

            return (neighbors);
        }

        private static float Area2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.X - a.X) * (c.Y - a.Y) -
                        (c.X - a.X) * (b.Y - a.Y);
        }

        private unsafe void DistancePoint2Face(Vector3 p, int f)
        {

            int i, j, k = 0;
            float d, l = 0, m, n;
            Vector2 aa = default, bb = default, cc = default, pp = default;
            Vector3 u, v;


            // Save Current Face Vertices Indices
            int a = mf[f][0];
            if ((p - mv[a].Position).LengthSquared() == 0) return;

            int b = mf[f][1];
            if ((p - mv[b].Position).LengthSquared() == 0) return;

            int c = mf[f][2];
            if ((p - mv[c].Position).LengthSquared() == 0) return;



            _FacesTested++;

            /////////////////////////////////////////////
            // Distance Point To Plane
            /////////////////////////////////////////////
            d = DistancePoint2Plane(p, mfn[f], mp[f]);
            // If Distance < Error
            if (MathF.Abs(d) < neighbors.Distance())
            {
                float* pMfn = stackalloc[] { mfn[f].X, mfn[f].Y, mfn[f].Z };
                // Find largest component
                for (i = 0; i < 3; i++)
                {
                    m = MathF.Abs(pMfn[i]); // Current Component
                    if (m > l)              // Biggest component
                    {
                        l = m;      // Save value
                        k = i;      // Save component indice
                    }
                }
                // Projected Point on plane
                u = p - mfn[f] * d;
                // project out coordinate "k"
                j = 0;

                float* pmva = stackalloc[] { mv[a].Position.X, mv[a].Position.Y, mv[a].Position.Z };
                float* pmvb = stackalloc[] { mv[b].Position.X, mv[b].Position.Y, mv[b].Position.Z };
                float* pmvc = stackalloc[] { mv[c].Position.X, mv[c].Position.Y, mv[c].Position.Z };
                float* pu = stackalloc[] { u.X, u.Y, u.Z };
                float* paa = stackalloc[] { aa.X, aa.Y };
                float* pbb = stackalloc[] { bb.X, bb.Y };
                float* pcc = stackalloc[] { cc.X, cc.Y };
                float* ppp = stackalloc[] { pp.X, pp.Y };
                for (i = 0; i < 3; i++) 
                {
                    if (i != k)
                    {
                        paa[j] = pmva[i];
                        pbb[j] = pmvb[i];
                        pcc[j] = pmvc[i];
                        ppp[j] = pu[i];
                        j++;
                    }
                }

                aa.X = paa[0]; aa.Y = paa[1];

                bb.X = pbb[0]; bb.Y = pbb[1];

                cc.X = pcc[0]; cc.Y = pcc[1];
                pp.X = ppp[0]; pp.Y = ppp[1];

                // compute areas
                l = Area2D(pp, aa, bb);
                m = Area2D(pp, bb, cc);
                n = Area2D(pp, cc, aa);
                // Test if projected point is in face
                if (((l > 0) && (m > 0) && (n > 0)) || ((l < 0) && (m < 0) && (n < 0)))
                {

                    v = Vector3.Normalize(mv[b].Position - mv[a].Position);

                    // both ^ corss or dot
                    l = Vector3.Cross(v, (u - mv[a].Position)).Length() / Vector3.Cross(v, (mv[c].Position - mv[a].Position)).Length();

                    if (l > 1) l = 1;
                    if (l < 0) l = 0;


                    v = Vector3.Lerp(mv[a].Position, mv[c].Position, l);

                    m = (u - v).Length() / (Vector3.Lerp(mv[b].Position, mv[c].Position, l) - v).Length();

                    d = MathF.Abs(d);
                    if (!((l < 0) || (l > 1) || (m < 0) || (m > 1)))
                    {
                        if (d == neighbors.Distance())
                            neighbors.AddFace(u, f, l, m);
                        else neighbors.NewFace(d, u, f, l, m);
                        // if distance = 0 -> quit
                        if (d == 0) return;
                    }
                }
            }

            /////////////////////////////////////////////
            // Distance Point To Edge
            /////////////////////////////////////////////
            u = p - mv[a].Position;
            v = mv[b].Position - mv[a].Position;
            l = v.Length();
            // ^ cross or dot
            d = Vector3.Cross(v, u).Length() / l;
            if (d < neighbors.Distance())
            {

                // | dot or cross
                m = Vector3.Dot(u, v) / l;
                if ((m > 0.0f) && (m < l))
                {
                    m /= l;
                    v = Vector3.Lerp(mv[a].Position, mv[b].Position, m);
                    if (d == neighbors.Distance())
                        neighbors.AddEdge(v, f, 0, m);
                    else neighbors.NewEdge(d, v, f, 0, m);
                    // if distance = 0 -> quit
                    if (d == 0) return;
                }
            }

            u = p - mv[b].Position;
            v = mv[c].Position - mv[b].Position;
            l = v.Length();
            // ^ cross or dot
            d = Vector3.Cross(v, u).Length() / l;
            if (d < neighbors.Distance())
            {
                // | dot or cross
                m = Vector3.Dot(u, v) / l;

                if ((m > 0.0f) && (m < l))
                {
                    m /= l;
                    v = Vector3.Lerp(mv[b].Position, mv[c].Position, m);
                    if (d == neighbors.Distance())
                        neighbors.AddEdge(v, f, 1, m);
                    else neighbors.NewEdge(d, v, f, 1, m);
                    // if distance = 0 -> quit
                    if (d == 0) return;
                }
            }

            u = p - mv[c].Position;
            v = mv[a].Position - mv[c].Position;
            l = v.Length();
            // ^ is dot or cross
            d = Vector3.Cross(v, u).Length() / l;
            if (d < neighbors.Distance())
            {
                // | dot or cross
                m = Vector3.Dot(u, v) / l;
                if ((m > 0.0f) && (m < l))
                {
                    m /= l;
                    v = Vector3.Lerp(mv[c].Position, mv[a].Position, m);
                    if (d == neighbors.Distance())
                        neighbors.AddEdge(v, f, 2, m);
                    else neighbors.NewEdge(d, v, f, 2, m);
                    // if distance = 0 -> quit
                    if (d == 0) return;
                }
            }
        }

        public int FacesTestedNumber() { return _FacesTested; }

        private void AddOnePoint(int n, int x, int y, int z)
        {
            if (m_pCell[x][y][z] == null)
            {
                m_pCell[x][y][z] = new Cell3D()
                {
                    v = n,
                    f = -1
                };
            }
            else
            {
                pCell = m_pCell[x][y][z];
                while (pCell.next != null) pCell = pCell.next;
                pCell.next = new Cell3D()
                {
                    v = n,
                    f = -1
                };
            }
        }
    }
}