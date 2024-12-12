using System.Numerics;

namespace SDL_Vulkan_CS.Comp302
{
    public struct Bounds
    {
        private Vector3 center;
        private Vector3 extents;
        public Vector3 Min => center - extents;

        public Vector3 Size
        {   
            readonly get => extents * 2f;
            set => extents = value * 0.5f;
        }

        public Bounds(Vector3 center, Vector3 extents)
        {
            this.center = center;
            this.extents = extents;
        }
    }
}