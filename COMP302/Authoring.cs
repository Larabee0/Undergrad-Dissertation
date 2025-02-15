using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COMP302.Decimator;
using Planets;
using VECS;
using VECS.ECS;
using VECS.ECS.Presentation;
using VECS.ECS.Transforms;
using VECS.LowLevel;
using Vortice.Vulkan;
using Mesh = VECS.Mesh;

namespace COMP302
{
    public static class Authoring
    {
        private static readonly int subdivisionsA = 20;
        private static readonly int subdivisionsB = 20;

        private static readonly bool QuadricSimplification = true;
        private static readonly bool enableDevation = true;
        private static readonly bool parallelDevation = true;

        private static int tileIterCount = 10;
        private static readonly bool interAllTiles = true;

        public static void Run()
        {
            GenerateAndCopyBack(out Mesh[] aMeshes, out Mesh[] bMeshes);

            //Mesh.SaveToFile(aMeshes[0], System.IO.Path.Combine(Mesh.DefaultMeshPath, "Tile_Test.obj"));

            //var mesh = Mesh.LoadModelFromFile(Mesh.GetMeshInDefaultPath("Tile_Test.obj"))[0];
            //aMeshes[0].Vertices = mesh.Vertices;
            //aMeshes[0].Indices = mesh.Indices;
            //aMeshes[0].Optimise();
            //aMeshes[0].FlushMesh();
            if (QuadricSimplification)
                DoQuadricSimplification(aMeshes);

            if (enableDevation)
                DoDevation(bMeshes, aMeshes);

        }

        private static void DoQuadricSimplification(Mesh[] aMeshes)
        {
            var parameter = new EdgeCollapseParameter
            {
                UsedProperty = VertexProperty.UV0
            };
            for (int i = 0; i < aMeshes.Length; i++)
            {
                var conditions = new TargetConditions
                {
                    faceCount = aMeshes[i].IndexCount / 3 / 2
                };
                var meshDecimation = new UnityMeshDecimation();
                meshDecimation.Execute(aMeshes[i], parameter, conditions);
                meshDecimation.ToMesh(aMeshes[i]);
                //aMeshes[i].Optimise();
                aMeshes[i].FlushMesh();
            }
        }

        private static void GenerateAndCopyBack(out Mesh[] aMeshes, out Mesh[] bMeshes)
        {
            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(ModelPushConstantData));

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();

            var a = CreateSubdividedPlanet(World.DefaultWorld.EntityManager, lit, subdivisionsA);
            var b = CreateSubdividedPlanet(World.DefaultWorld.EntityManager, lit, subdivisionsB);

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(-5, 0, 0) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(15, 0, 0) });

            var now = DateTime.Now;
            var allMeshes = GetMeshesFrom(World.DefaultWorld.EntityManager, a, b);
            aMeshes = allMeshes[0];
            bMeshes = allMeshes[1];
            var delta = DateTime.Now - now;
            Console.WriteLine(string.Format("Copy back time: {0}ms", delta.TotalMilliseconds));
        }

        private static void DoDevation( Mesh[] aMeshes, Mesh[] bMeshes)
        {
            if (interAllTiles)
            {
                tileIterCount = aMeshes.Length;
            }
            var now = DateTime.Now;
            string[] stats = new string[tileIterCount];
            for (int i = 0; i < tileIterCount; i++)
            {
                for (int j = 0; j < bMeshes[i].Vertices.Length; j++)
                {
                    bMeshes[i].Vertices[j].Elevation = 0;
                }

                var devation = new Deviation();

                devation.Initialization(aMeshes[i], bMeshes[i]);
                devation.Compute(parallelDevation);
                stats[i] = devation.GetStatisticsString();
            }

            var delta = DateTime.Now - now;

            for (int i = 0; i < tileIterCount; i++)
            {
                Console.WriteLine(stats[i]);
                aMeshes[i].FlushMesh();
                bMeshes[i].FlushMesh();
            }
            Console.WriteLine(string.Format("Devation Calc: {0}ms", delta.TotalMilliseconds));
        }

        public static Mesh[][] GetMeshesFrom(EntityManager entityManager,params Entity[] hierarhcy)
        {
            List<Entity> entities = [];
            List<int> offsets = [];
            for (int i = 0; i < hierarhcy.Length; i++)
            {
                if (!entityManager.HasComponent<Children>(hierarhcy[i]))
                {
                    continue;
                }
                entities.AddRange(entityManager.GetComponent<Children>(hierarhcy[i]).Value);
                if (i != 0)
                {
                    offsets.Add(entities.Count - offsets[^1]);
                }
                else
                {
                    offsets.Add(entities.Count);
                }
                
            }
            offsets.Add(entities.Count - offsets[^1]);
            Mesh[] meshes = new Mesh[entities.Count];

            GPUBuffer<VECS.Vertex>[] vertexBuffers = new GPUBuffer<VECS.Vertex>[meshes.Length];
            GPUBuffer<uint>[] indexBuffers = new GPUBuffer<uint>[meshes.Length];

            VkCommandBuffer copyBufferCmd = GraphicsDevice.Instance.BeginSingleTimeCommands();

            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i] = Mesh.GetMeshAtIndex(entityManager.GetComponent<MeshIndex>(entities[i]).Value);
                meshes[i].EnsureAlloc();
                vertexBuffers[i] = new GPUBuffer<VECS.Vertex>((uint)meshes[i].VertexCount,VkBufferUsageFlags.TransferDst,true);
                indexBuffers[i] = new GPUBuffer<uint>((uint)meshes[i].IndexCount,VkBufferUsageFlags.TransferDst,true);

                meshes[i].VertexBuffer.CopyTo(copyBufferCmd,vertexBuffers[i]);
                meshes[i].IndexBuffer.CopyTo(copyBufferCmd, indexBuffers[i]);

            }
            GraphicsDevice.Instance.EndSingleTimeCommands(copyBufferCmd);

            Parallel.For(0, meshes.Length, (int i) =>
            {
                vertexBuffers[i].ReadFromBuffer(meshes[i].Vertices);
                indexBuffers[i].ReadFromBuffer(meshes[i].Indices);
            });

            for (int i = 0; i < meshes.Length; i++)
            {
                vertexBuffers[i].Dispose();
                indexBuffers[i].Dispose();
            }

            Mesh[][] splits = new Mesh[hierarhcy.Length][];

            for (int i = 0;i < hierarhcy.Length; i++)
            {
                splits[i] = new Mesh[offsets[i]];
                Array.Copy(meshes,((i == 0) ? 0 : offsets[i-1]), splits[i],0, splits[i].Length);
            }

            return splits;
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
            var cubeUvMesh = Mesh.LoadModelFromFile(Mesh.GetMeshInDefaultPath("blender-cube.obj"));
            var flatVaseMesh = Mesh.LoadModelFromFile(Mesh.GetMeshInDefaultPath("blender-sphere.obj"));

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();

            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(ModelPushConstantData));


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
