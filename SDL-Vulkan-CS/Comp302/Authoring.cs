using SDL_Vulkan_CS.Artifact;
using SDL_Vulkan_CS.ECS;
using SDL_Vulkan_CS.ECS.Presentation;
using SDL_Vulkan_CS.VulkanBackend;
using System;
using System.Threading.Tasks;

namespace SDL_Vulkan_CS.Comp302
{
    public static class Authoring
    {
        private static readonly int subdivisionsA = 30;
        private static readonly int subdivisionsB = 60;

        private static readonly bool parallelDevation = false;

        private static int tileIterCount = 10;
        private static readonly bool interAllTiles = true;

        public static void Run()
        {
            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(SimplePushConstantData));

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();

            var a = CreateSubdividedPlanet(World.DefaultWorld.EntityManager,lit,subdivisionsA);
            var b = CreateSubdividedPlanet(World.DefaultWorld.EntityManager,lit, subdivisionsB);

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(-5, 0, 0) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(15, 0, 0) });

            var aMeshes = GetMeshesFrom(World.DefaultWorld.EntityManager, a);
            var bMeshes = GetMeshesFrom(World.DefaultWorld.EntityManager, b);

            var devations = new Deviation[aMeshes.Length];
            
            if(interAllTiles)
            {
                tileIterCount = aMeshes.Length;
            }

            var now = DateTime.Now;
            if (parallelDevation)
            {
                Parallel.For(0, tileIterCount, (int i) =>
                {
                    for (int j = 0; j < bMeshes[i].VertexCount; j++)
                    {
                        bMeshes[i].Vertices[j].Elevation = 0;
                    }

                    devations[i] = new Deviation();

                    devations[i].Initialization(aMeshes[i], bMeshes[i]);
                    devations[i].Compute();
                    devations[i].CleanUp();

                });
            }
            else
            {
                for (int i = 0; i < tileIterCount; i++)
                {
                    for (int j = 0; j < bMeshes[i].VertexCount; j++)
                    {
                        bMeshes[i].Vertices[j].Elevation = 0;
                    }

                    devations[i] = new Deviation();

                    devations[i].Initialization(aMeshes[i], bMeshes[i]);
                    devations[i].Compute();
                    devations[i].CleanUp();
                }
            }

            var delta = DateTime.Now - now;

            for (int i = 0; i< tileIterCount; i++)
            {
                // var devations = new Deviation();
                // 
                // devations.Initialization(aMeshes[i], bMeshes[i]);
                // devations.Compute();
                // 
                // for (int j = 0; j < bMeshes[i].VertexCount; j++)
                // {
                //     bMeshes[i].Vertices[j].Elevation = 0;
                // }
                Console.WriteLine(devations[i].GetStatisticsString());
                aMeshes[i].FlushVertexBuffer();
                bMeshes[i].FlushVertexBuffer();
            }
            Console.WriteLine(string.Format("Devation Calc: {0}ms", delta.TotalMilliseconds));
            //Debugger.Break();
        }

        public static Mesh[] GetMeshesFrom(EntityManager entityManager,Entity hierarhcy)
        {
            if (!entityManager.HasComponent<Children>(hierarhcy))
            {
                return null;
            }
            var children = entityManager.GetComponent<Children>(hierarhcy);
            Mesh[] meshes = new Mesh[children.Value.Length];
            for (int i = 0; i < children.Value.Length; i++)
            {
                meshes[i] = Mesh.GetMeshAtIndex(entityManager.GetComponent<MeshIndex>(children.Value[i]).Value);

                meshes[i].CopyVertexBufferBack();
                meshes[i].CopyIndexBufferBack();
            }

            return meshes;
        }

        private static Entity CreateSubdividedPlanet(EntityManager entityManager, Material material, int subdivisons)
        {

            var planet = entityManager.CreateEntity();
            entityManager.AddComponent(planet, new Translation() { Value = new(0, 0f, 0) });
            entityManager.AddComponent(planet, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent<Children>(planet);
            entityManager.AddComponent(planet, new MaterialIndex { Value = Material.GetIndexOfMaterial(material) });

            ArtifactAuthoring.InitialiseTiles(entityManager, planet, subdivisons);

            entityManager.RemoveComponentFromHierarchy<DoNotRender>(planet);
            entityManager.RemoveComponentFromHierarchy<Prefab>(planet);

            ArtifactAuthoring.GeneratePlanet(planet, PlanetPresets.ShapeGeneratorFixedEarthLike());

            var children = entityManager.GetComponent<Children>(planet);

            for (int i = 0; i < children.Value.Length; i++)
            {
                entityManager.AddComponent(children.Value[i], new MaterialIndex { Value = Material.GetIndexOfMaterial(material) });
            }

            return planet;
        }

        public static void LoadTestScene(EntityManager entityManager)
        {
            var cubeUvMesh = Mesh.LoadModelFromFile(GraphicsDevice.Instance, Mesh.GetMeshInDefaultPath("blender-cube.obj"));
            var flatVaseMesh = Mesh.LoadModelFromFile(GraphicsDevice.Instance, Mesh.GetMeshInDefaultPath("blender-sphere.obj"));

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();

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
