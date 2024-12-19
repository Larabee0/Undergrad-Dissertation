using SDL_Vulkan_CS.ECS;
using SDL_Vulkan_CS.ECS.Presentation;
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
            //LoadTestScene(World.DefaultWorld.EntityManager);
            var devationCal = new Deviation();
            devationCal.Initialization(Mesh.Meshes[0], Mesh.Meshes[^1]);
            devationCal.Compute();
        }


        public static void LoadTestScene(EntityManager entityManager)
        {
            var cubeUvMesh = Mesh.LoadModelFromFile(GraphicsDevice.Instance, Mesh.GetMeshInDefaultPath("blender-cube.obj"));
            var flatVaseMesh = Mesh.LoadModelFromFile(GraphicsDevice.Instance, Mesh.GetMeshInDefaultPath("blender-sphere.obj"));

            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(SimplePushConstantData));


            var cube = entityManager.CreateEntity();
            entityManager.AddComponent(cube, new Translation() { Value = new(1.5f, -1.5f, 0) });
            entityManager.AddComponent(cube, new MeshIndex() { Value = Mesh.GetIndexOfMesh(cubeUvMesh[0]) });
            entityManager.AddComponent(cube, new MaterialIndex() { Value = Material.GetIndexOfMaterial(lit) });

            var sphere = entityManager.CreateEntity();
            entityManager.AddComponent(sphere, new Translation() { Value = new(-1.5f, 1.5f, 0) });
            entityManager.AddComponent(sphere, new MeshIndex() { Value = Mesh.GetIndexOfMesh(flatVaseMesh[0]) });
            entityManager.AddComponent(sphere, new MaterialIndex() { Value = Material.GetIndexOfMaterial(lit) });

        }
    }
}
