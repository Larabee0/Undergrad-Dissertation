using System;
using System.Linq;
using VECS;
using System.Numerics;
using System.Threading.Tasks;

namespace COMP302
{
    /// <summary>
    /// C++ Source: https://meshdev.sourceforge.net/
    /// LICENSE GNU General Public License
    /// Translated to C# by William Vickers Hastings
    /// </summary>
    public class Deviation
    {
        private readonly static int[][] Edge2Vertex = [[0, 1], [1, 2], [2, 0]];
        private DirectSubMesh ma;
        private DirectSubMesh mb;

        private int mavn;
        private int mbvn;

        private int mafn;
        private int mbfn;

        float[] dev; // Deviation

        private float mindev;
        private float maxdev;
        private float vardev;
        private float meandev;
        //private float meddev;
        private float rmsdev;
        private float dev_bound;

        private Bounds bb;

        private UniformGrid ug;

        //private float step;
        //private Vector3 sampleu, samplev; // Vectors for sampling
        //private Sample[] samples;
        //private int snum;

        public Deviation()
        {

        }

        public bool Initialization(DirectSubMesh a, DirectSubMesh b,float GridSize = 0.5f)
        {
            ma = a;
            mb = b;

            mavn = (int)ma.VertexCount;
            mbvn = (int)mb.VertexCount;

            mafn = ma.Faces.Length;
            mbfn = mb.Faces.Length;

            ma.RecalculateRenderBounds();
            bb = ma.Bounds.Bounds;

            mb.RecalculateRenderBounds();
            bb = mb.Bounds.Bounds;

            ug = new UniformGrid(mb, bb, bb.Size.Length() * GridSize * 0.01f);

            return true;
        }

        public bool Compute(bool normalDeviation, bool parallel)
        {
            if (normalDeviation)
            {
                return MeshDeviation(parallel, [..ma.GetVertexDataSpan<Vector3>(VertexAttribute.Normal)], [.. mb.GetVertexDataSpan<Vector3>(VertexAttribute.Normal)]);
            }
            else
            {
                return GeometricDeviation(parallel);
            }
                
        }


        public void CleanUp()
        {
            ug = null;
        }

        private bool GeometricDeviation(bool parallel)
        {
            dev = new float[mavn];
            Vector3[] vertices = [..ma.Vertices];
            if (parallel)
            {

                Parallel.For(0, dev.Length, (int i) =>
                {
                    dev[i] = ug.NearestNeighbors(vertices[i]).Distance();
                });
            }
            else
            {
                for (int i = 0; i < dev.Length; i++)
                {
                    dev[i] = ug.NearestNeighbors(vertices[i]).Distance();
                }
            }

            Statistics();
            Deviation2Material();

            return true;
        }

        public bool MeshDeviation(bool parallel, Vector3[] aa, Vector3[]ab)
        {
            dev = new float[mavn];
            Vector3[] vertices = [.. ma.Vertices];

            if (parallel)
            {

                Parallel.For(0, mavn, (int i) =>
                {
                    NormalDeviation(aa, ab, vertices, i);
                });
            }
            else
            {
                for (int i = 0; i < mavn; i++)
                {
                    NormalDeviation(aa, ab, vertices, i);
                }
            }


            Statistics();
            Deviation2Material();
            return true;
        }

        private void NormalDeviation(Vector3[] aa, Vector3[] ab, Vector3[] vertices, int i)
        {
            Neighbor nearest = ug.NearestNeighbors(vertices[i]).Neighbors();
            float d;
            while (nearest != null)
            {
                if (nearest.v != -1)
                {
                    d = (aa[i] - ab[nearest.v]).Length();
                }
                else if (nearest.e != -1)
                {

                    uint a = mb.Face(nearest.f, Edge2Vertex[nearest.e][0]);
                    uint b = mb.Face(nearest.f, Edge2Vertex[nearest.e][1]);

                    d = (aa[i] - Vector3.Lerp(ab[a], ab[b], nearest.r1)).Length();
                }
                else
                {
                    uint a = mb.Face(nearest.f, 0);
                    uint b = mb.Face(nearest.f, 1);
                    uint c = mb.Face(nearest.f, 2);

                    d = (aa[i] - Vector3.Lerp(Vector3.Lerp(ab[a], ab[c], nearest.r1), Vector3.Lerp(ab[b], ab[c], nearest.r1), nearest.r2)).Length();
                }
                if (d < dev[i])
                {
                    dev[i] = d;
                }
                nearest = nearest.next;
            }
        }

        public bool Statistics()
        {
            if (dev.Length == 0)
            {
                return false;
            }

            mindev = dev.Min();
            // Maximum
            maxdev = dev.Max();
            // Mean
            meandev = dev.Sum() / dev.Length;
            vardev = 0;
            rmsdev = 0;
            for (int i = 0; i < dev.Length; i++)
            {
                rmsdev += dev[i] * dev[i];
                vardev += Sqr(dev[i] - meandev);
            }
            vardev /= dev.Length - 1;
            rmsdev /= dev.Length;
            rmsdev = MathF.Sqrt(rmsdev);

            return true;
        }

        public string GetStatisticsString()
        {
            return string.Format("minDev {0}, maxDev {1}, meanDev {2}, varDev {3}, rmsDev {4}", mindev, maxdev, meandev, vardev, rmsdev);
        }

        public string GetCSVStatisticRow()
        {
            return string.Format("{0}, {1}, {2}", mindev, maxdev, meandev);
        }

        private static float Sqr(float x)
        {
            return x * x;
        }


        public void Deviation2Material()
        {
            if (dev_bound <= 0)
            {
                dev_bound = maxdev;
            }
            if (dev_bound == 0)
            {
                // Avoid division by 0
                // if maximum deviation is null
                // (no deviation)
                dev_bound = 1;
            }

            var uvs = ma.GetVertexDataSpan<Vector2>(VertexAttribute.TexCoord0);
            for (int i = 0; i < mavn; i++)
            {
                // Normalize deviation values
                uvs[i].X = (dev[i] * 1) / dev_bound;
            }
        }

    }
}