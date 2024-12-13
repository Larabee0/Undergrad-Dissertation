namespace SDL_Vulkan_CS.Comp302
{
    /// <summary>
    /// C++ Source: https://meshdev.sourceforge.net/
    /// LICENSE GNU General Public License
    /// Translated to C# by William Vickers Hastings
    /// </summary>
    public class Sample
    {
        private int height;
        private int[] lw;
        private float[][] dev;

        public int Height => height;

        public void SetHeight(int height)
        {
            this.height = height;
            lw = new int[height];
        }

        public void InitDev()
        {
            dev = new float[height][];
            for (int i = 0; i < height; i++)
            {
                dev[i] = new float[lw[i]];
            }

        }

    }
}