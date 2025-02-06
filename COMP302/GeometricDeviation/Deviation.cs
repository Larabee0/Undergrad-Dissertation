using System;
using System.Linq;
using VECS;
using System.Numerics;

namespace COMP302
{
    /// <summary>
    /// C++ Source: https://meshdev.sourceforge.net/
    /// LICENSE GNU General Public License
    /// Translated to C# by William Vickers Hastings
    /// </summary>
    public class Deviation
    {
        private Mesh ma;
        private Mesh mb;

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

        public bool Initialization(Mesh a, Mesh b, float SampleStep = 0, float GridSize = 0.5f)
        {
            ma = a;
            mb = b;

            mavn = ma.VertexCount;
            mbvn = mb.VertexCount;

            mafn = ma.Faces.Length;
            mbfn = mb.Faces.Length;

            mb.ComputeFaceNormals();

            ma.RecalculateBounds();
            bb = ma.Bounds;

            // if (SampleStep != 0)
            // {
            //     step = bb.Size.Length() * SampleStep * 0.01f;
            //     samples = new Sample[mafn];
            // }
            // else
            // {
            //     step = 0;
            // }
            // snum = 0;
            mb.RecalculateBounds();
            bb = mb.Bounds;

            ug = new UniformGrid(mb, bb, bb.Size.Length() * GridSize * 0.01f);

            return true;
        }

        public bool Compute(bool parallel)
        {
            return GeometricDeviation(parallel);
        }


        public void CleanUp()
        {
            ug = null;
        }

        private bool GeometricDeviation(bool parallel)
        {
            dev = new float[mavn];

            if (parallel)
            {
                Parallel.For(0, dev.Length, (int i) =>
                {
                    dev[i] = ug.NearestNeighbors(ma.Vertices[i].Position).Distance();
                });
            }
            else
            {
                for (int i = 0; i < dev.Length; i++)
                {
                    dev[i] = ug.NearestNeighbors(ma.Vertices[i].Position).Distance();
                }
            }

            Statistics();
            Deviation2Material();

            return true;
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
            vardev /= (float)(dev.Length - 1);
            rmsdev /= (float)dev.Length;
            rmsdev = MathF.Sqrt(rmsdev);

            return true;
        }

        public string GetStatisticsString()
        {
            return(string.Format("minDev {0}, maxDev {1}, meanDev {2}, varDev {3}, rmsDev {4}", mindev, maxdev, meandev, vardev, rmsdev));
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

            for (int i = 0; i < mavn; i++)
            {
                // Normalize deviation values
                ma.Vertices[i].Elevation = (dev[i]*1) / dev_bound;
            }
        }

    }
}