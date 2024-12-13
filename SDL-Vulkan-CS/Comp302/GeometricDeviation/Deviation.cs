using System;
using System.Linq;
using SDL_Vulkan_CS.VulkanBackend;
using System.Numerics;

namespace SDL_Vulkan_CS.Comp302
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
        private float meddev;
        private float rmsdev;
        private float dev_bound;

        private Bounds bb;

        private UniformGrid ug;

        private float step;
        private Vector3 sampleu, samplev; // Vectors for sampling
        private Sample[] samples;
        private int snum;

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

            if (SampleStep != 0)
            {
                step = bb.Size.Length() * SampleStep * 0.01f;
                samples = new Sample[mafn];
            }
            else
            {
                step = 0;
            }
            snum = 0;
            mb.RecalculateBounds();
            bb = mb.Bounds;

            ug = new UniformGrid(mb, bb, bb.Size.Length() * GridSize * 0.01f);

            return true;
        }

        public bool Compute()
        {
            return GeometricDeviation();
        }

        private bool GeometricDeviation()
        {
            dev = new float[mavn];

            for (int i = 0; i < dev.Length; i++)
            {
                dev[i] = ug.NearestNeighbors(ma.Vertices[i].Position).Distance();
            }

            Statistics();
            Deviation2Material();

            return true;
        }

        private bool Statistics()
        {
            int i;
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
            for (i = 0; i < dev.Length; i++)
            {
                rmsdev += dev[i] * dev[i];
                vardev += Sqr(dev[i] - meandev);
            }
            vardev /= (float)(dev.Length - 1);
            rmsdev /= (float)dev.Length;
            rmsdev = MathF.Sqrt(rmsdev);

            Console.WriteLine(string.Format("minDev {0}, maxDev {1}, meanDev {2}, varDev {3}, rmsDev {4}", mindev, maxdev, meandev, vardev, rmsdev));
            return true;
        }

        private float Sqr(float x)
        {
            return x * x;
        }


        private void Deviation2Material()
        {
            Vector3[] colours = new Vector3[mavn];

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
                //colours[i] = Deviation2Color(dev[i] / dev_bound);
            }

             ma.FlushVertexBuffer();
        }

        private static Vector3 Deviation2Color(float d)
        {
            if (d < 0) return new Vector3(0, 0, 0);
            else if (d < 0.25f) return new Vector3(0, d * 4.0f, 1);
            else if (d < 0.50f) return new Vector3(0, 1, 1 - (d - 0.25f) * 4.0f);
            else if (d < 0.75f) return new Vector3((d - 0.5f) * 4.0f, 1, 0);
            else if (d < 1) return new Vector3(1, 1.0f - (d - 0.75f) * 4.0f, 0);
            return new Vector3(1, 0, 0);
        }
    }
}