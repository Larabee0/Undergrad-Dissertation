using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using COMP302.Decimator;
using Planets;
using Planets.Generator;
using VECS;
using VECS.DataStructures;
using VECS.ECS;
using VECS.ECS.Presentation;
using VECS.ECS.Transforms;

namespace COMP302
{
    public static class Authoring
    {
        private static readonly int subdivisionsA = 50;
        private static readonly int subdivisionsB = 25;
        private static readonly int subdivisionsC = 50;
        private static readonly int subdivisionsD = 50;

        private static readonly bool QuadricSimplification = true;
        private static readonly bool enableDevation = true;
        private static readonly bool parallelDevation = true;

        private static int tileIterCount = 10;
        private static readonly bool interAllTiles = true;

        private static readonly Stopwatch _stopwatch = new();

        public static void Run()
        {
            GenerateAndCopyBack(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes);

            if (QuadricSimplification)
                DoQuadricSimplification(dMeshes);



            if (enableDevation)
            {
                DoDevation(aMeshes, bMeshes);
                DoDevation(cMeshes, dMeshes);
            }
            GetStats(aMeshes);
            GetStats(bMeshes);
            GetStats(dMeshes);
        }

        private static void GetStats(DirectSubMesh[] meshes)
        {
            uint verts = 0;
            uint tris = 0;
            for (int i = 0; i < meshes[0].DirectMeshBuffer.SubMeshInfos.Length; i++)
            {
                verts += meshes[0].DirectMeshBuffer.SubMeshInfos[i].VertexCount;
                tris += meshes[0].DirectMeshBuffer.SubMeshInfos[i].IndexCount;
            }

            Console.WriteLine(string.Format("Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", verts, tris, tris / 3));
        }

        private static void DoQuadricSimplification(DirectSubMesh[] aMeshes)
        {
            aMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            _stopwatch.Restart();
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 16
            };
            //aMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            Parallel.For(0, aMeshes.Length,parallelOptions, (int i) =>
            {
                Simplify(aMeshes[i]);
            });

            aMeshes[0].DirectMeshBuffer.FlushAll();
            _stopwatch.Stop();
            Console.WriteLine(string.Format("Simplification time: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        private static void Simplify(DirectSubMesh mesh)
        {
            var parameter = new EdgeCollapseParameter
            {
                UsedProperty = VertexProperty.UV0,
                PreserveBoundary = true,
                NormalCheck = false,
                BoundaryWeight = 0.5f
            };
            var conditions = new TargetConditions
            {
                faceCount = (int)mesh.IndexCount / 3 / 2
            };
            var meshDecimation = new UnityMeshDecimation();
            meshDecimation.Execute(mesh, parameter, conditions);
            meshDecimation.ToMesh(mesh);
        }

        private static void GenerateAndCopyBack(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes)
        {
            VertexAttributeDescription[] vertexAttributeDescriptions = [
                new(VertexAttribute.Position,VertexAttributeFormat.Float3,0,0,0),
                new(VertexAttribute.Normal,VertexAttributeFormat.Float3,0,1,1),
                new(VertexAttribute.TexCoord0,VertexAttributeFormat.Float2,0,2,2),
            ];

            var bindingDescriptions = DirectMeshBuffer.GetBindingDescription(vertexAttributeDescriptions);
            var attributeDescriptions = DirectMeshBuffer.GetAttributeDescriptions(vertexAttributeDescriptions);
            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(ModelPushConstantData), bindingDescriptions, attributeDescriptions);

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();
            var shapeGenerator = PlanetPresets.ShapeGeneratorFixedEarthLike();
            shapeGenerator.RandomiseSettings();
            var a = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, lit, subdivisionsA);
            var b = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, lit, subdivisionsB);
            var c = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, lit, subdivisionsC);
            var d = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, lit, subdivisionsD);

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(5f, 0, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(5, 0, -5f) });
            World.DefaultWorld.EntityManager.SetComponent(c, new Translation() { Value = new(-5f, 0, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(d, new Translation() { Value = new(-5f, 0, -5f) });

            _stopwatch.Restart();
            var allMeshes = GetMeshesFrom(World.DefaultWorld.EntityManager, a, b, c, d);
            aMeshes = allMeshes[0];
            bMeshes = allMeshes[1];
            cMeshes = allMeshes[2];
            dMeshes = allMeshes[3];
            aMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            bMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            cMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            dMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            _stopwatch.Stop();
            Console.WriteLine(string.Format("Copy back time: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        private static void DoDevation(DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes)
        {
            if (interAllTiles)
            {
                tileIterCount = aMeshes.Length;
            }
            _stopwatch.Restart();
            string[] stats = new string[tileIterCount];
            aMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            bMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            for (int i = 0; i < tileIterCount; i++)
            {
                var uvs = bMeshes[i].GetVertexDataSpan<Vector2>(VertexAttribute.TexCoord0);
                for (int j = 0; j < bMeshes[i].Vertices.Length; j++)
                {
                    uvs[j].X = 0;
                }

                var devation = new Deviation();

                devation.Initialization(aMeshes[i], bMeshes[i]);
                devation.Compute(parallelDevation);
                stats[i] = devation.GetStatisticsString();
            }

            _stopwatch.Stop();

            for (int i = 0; i < tileIterCount; i++)
            {
                Console.WriteLine(stats[i]);
            }
            aMeshes[0].DirectMeshBuffer.FlushAll();
            bMeshes[0].DirectMeshBuffer.FlushAll();
            DirectMeshBuffer.RecalcualteAllNormals(aMeshes[0].DirectMeshBuffer);
            DirectMeshBuffer.RecalcualteAllNormals(bMeshes[0].DirectMeshBuffer);
            Console.WriteLine(string.Format("Devation Calc: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        public static DirectSubMesh[][] GetMeshesFrom(EntityManager entityManager,params Entity[] hierarhcy)
        {
            //List<Entity> entities = [];
            //List<int> offsets = [];
            //for (int i = 0; i < hierarhcy.Length; i++)
            //{
            //    if (!entityManager.HasComponent<Children>(hierarhcy[i]))
            //    {
            //        continue;
            //    }
            //    entities.AddRange(entityManager.GetComponent<Children>(hierarhcy[i]).Value);
            //    if (i != 0)
            //    {
            //        offsets.Add(entities.Count);
            //    }
            //    else
            //    {
            //        offsets.Add(entities.Count);
            //    }
            //    
            //}
            //offsets.Add(entities.Count - offsets[^1]);
            //DirectSubMesh[] meshes = new DirectSubMesh[entities.Count];

            DirectSubMesh[][] splits = new DirectSubMesh[hierarhcy.Length][];
            //VkCommandBuffer copyBufferCmd = GraphicsDevice.Instance.BeginSingleTimeCommands();
            for (int i = 0; i < hierarhcy.Length; i++)
            {
                var entityMeshes = entityManager.GetComponent<Children>(hierarhcy[i]).Value;
                splits[i] = new DirectSubMesh[entityMeshes.Length];
                for (int j = 0; j < entityMeshes.Length; j++)
                {
                    splits[i][j] = DirectSubMesh.GetSubMeshAtIndex(entityManager.GetComponent<DirectSubMeshIndex>(entityMeshes[j]));
                }
                //meshes[i].EnsureAlloc();
                //vertexBuffers[i] = meshes[i].GetAllVertexBuffers();// new GPUBuffer<VECS.Vertex>((uint)meshes[i].VertexCount,VkBufferUsageFlags.TransferDst,true);
                //indexBuffers[i] = new GPUBuffer<uint>((uint)meshes[i].IndexCount,VkBufferUsageFlags.TransferDst,true);
            
                //meshes[i].VertexBuffer.CopyTo(copyBufferCmd,vertexBuffers[i]);
                //meshes[i].IndexBuffer.CopyTo(copyBufferCmd, indexBuffers[i]);
            
            }
            //GraphicsDevice.Instance.EndSingleTimeCommands(copyBufferCmd);
            

            //for (int i = 0; i < hierarhcy.Length; i++)
            //{
            //    splits[i] = new DirectSubMesh[offsets[i]];
            //    Array.Copy(meshes,((i == 0) ? 0 : offsets[i-1]), splits[i],0, splits[i].Length);
            //}

            return splits;
        }

        private static Entity CreateSubdividedPlanet(ShapeGenerator shapeGenerator,EntityManager entityManager, Material material, int subdivisons)
        {

            var planet = entityManager.CreateEntity();
            entityManager.AddComponent(planet, new Translation() { Value = new(0, 0f, 0) });
            entityManager.AddComponent(planet, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent<Children>(planet);
            entityManager.AddComponent(planet, new MaterialIndex { Value = Material.GetIndexOfMaterial(material) });

            ArtifactAuthoring.InitialiseTiles(entityManager, planet, subdivisons);

            entityManager.RemoveComponentFromHierarchy<DoNotRender>(planet);
            entityManager.RemoveComponentFromHierarchy<Prefab>(planet);

            ArtifactAuthoring.GeneratePlanet(planet, shapeGenerator);

            var children = entityManager.GetComponent<Children>(planet);

            for (int i = 0; i < children.Value.Length; i++)
            {
                entityManager.AddComponent(children.Value[i], new MaterialIndex { Value = Material.GetIndexOfMaterial(material) });
            }

            return planet;
        }

        public static void LoadTestScene(EntityManager entityManager)
        {
            var cubeUvMesh = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("blender-cube.obj"), [new VertexAttributeDescription(VertexAttribute.TexCoord0, VertexAttributeFormat.Float2)]);
            var flatVaseMesh = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("blender-sphere.obj"), [new VertexAttributeDescription(VertexAttribute.TexCoord0, VertexAttributeFormat.Float2)]);

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();

            var lit = new Material("devation_heat.vert", "devation_heat.frag", typeof(ModelPushConstantData));

            var cube = entityManager.CreateEntity();
            entityManager.AddComponent(cube, new Translation() { Value = new(1.5f, -1.5f, 0) });
            entityManager.AddComponent(cube, new DirectSubMeshIndex() { DirectMeshBuffer = DirectMeshBuffer.GetIndexOfMesh(cubeUvMesh[0].DirectMeshBuffer), SubMeshIndex = 0 });
            entityManager.AddComponent(cube, new MaterialIndex() { Value = Material.GetIndexOfMaterial(lit) });

            var sphere = entityManager.CreateEntity();
            entityManager.AddComponent(sphere, new Translation() { Value = new(-1.5f, 1.5f, 0) });
            entityManager.AddComponent(sphere, new DirectSubMeshIndex() { DirectMeshBuffer = DirectMeshBuffer.GetIndexOfMesh(flatVaseMesh[0].DirectMeshBuffer), SubMeshIndex = 0 });
            entityManager.AddComponent(sphere, new MaterialIndex() { Value = Material.GetIndexOfMaterial(lit) });

        }
    }
}
