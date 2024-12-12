using System.Numerics;

namespace SDL_Vulkan_CS.Comp302
{
    public class Neighbor
    {
        public Vector3 c;     // Coordinates
        public int v;      // Vertex if similar
        public int f;      // Face containing the point
        public int e;      // Edge containing the point
        public float r1;      // Ratio 1
        public float r2;      // Ratio 2
        public Neighbor next;   // Next neighbor
    }
}