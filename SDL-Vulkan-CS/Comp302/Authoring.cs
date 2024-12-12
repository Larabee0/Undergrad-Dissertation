using SDL_Vulkan_CS.VulkanBackend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDL_Vulkan_CS.Comp302
{
    public static class Authoring
    {
        public static void Run()
        {
            var devationCal = new Deviation();
            devationCal.Initialization(Mesh.Meshes[0], Mesh.Meshes[^1]);
            devationCal.Compute();
        }
    }
}
