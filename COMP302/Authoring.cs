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
        private static readonly int subdivisionsA = 50; // high res
        private static int subdivisionsB; // low res
        private static readonly int subdivisionsC = subdivisionsA; // high res
        private static readonly int subdivisionsD = 50; // simplified
        private static float inputReductionRate = 0.5f;
        private static float actualReductionRate;

        private static readonly bool QuadricSimplification = true;
        private static readonly bool enableDevation = true;
        private static readonly bool parallelDevation = true;

        private static readonly bool logSimplificationRMS = false;
        private static readonly bool logDeviations = false;

        private static int tileIterCount = 10;
        private static readonly bool interAllTiles = true;

        private static readonly Stopwatch _stopwatch = new();
        private static DirectMeshBuffer _reference;
        private static Material _deviationHeatMap;

        public static void Run()
        {
            inputReductionRate = Math.Clamp(inputReductionRate, 0, 1);
            CalculateActualSimplificationRates();
            GenerateAndCopyBack(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes);

            if (QuadricSimplification)
                DoQuadricSimplification(dMeshes);

            if (enableDevation)
            {
                Console.WriteLine();
                Console.WriteLine("Calculating Meshes B Deviations (High Res vs Low Res Generation");
                DoDevation(aMeshes, bMeshes);
                Console.WriteLine();
                Console.WriteLine("Calculating Meshes D Deviations (High Res vs Quadric Simplified)");
                DoDevation(cMeshes, dMeshes);
            }
            Console.WriteLine(string.Format("\nInput reduct-rate: {0}% | Actual: {1}% | Subdivisions (Mesh B) {2}", (inputReductionRate * 100f).ToString("00.00"), (actualReductionRate * 100f).ToString("00.00"), subdivisionsB));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Meshes A-B (High Res vs Low Res Generation");
            GetStats(aMeshes, bMeshes);
            Console.WriteLine();
            Console.WriteLine("Meshes C-D (High Res vs Quadric Simplified)");
            GetStats(cMeshes, dMeshes);
        }

        private static void CalculateActualSimplificationRates()
        {
            _reference = GetMeshesInChildren(World.DefaultWorld.EntityManager, CreateUnGeneratedPlanet(World.DefaultWorld.EntityManager, 0))[0].DirectMeshBuffer;
            int subdivisonsForB = subdivisionsA;
            uint currentIndexCount = GetSubdivisonCounts(0, subdivisionsA).Item2;
            int dstFromTarget = int.MaxValue;
            uint targetIndexCount = (uint)MathF.Floor(currentIndexCount * inputReductionRate);
            for (int i = subdivisionsA; i > 0; i--)
            {
                var newDstFromTarget = Math.Abs((int)targetIndexCount - (int)GetSubdivisonCounts(0, i).Item2);

                if (newDstFromTarget < dstFromTarget)
                {
                    subdivisonsForB = i;
                    dstFromTarget = newDstFromTarget;
                }
                else if (newDstFromTarget > dstFromTarget)
                {
                    break;
                }
            }
            var calculatedIndexCount = GetSubdivisonCounts(0, subdivisonsForB).Item2;

            actualReductionRate = (float)calculatedIndexCount / (float)currentIndexCount;


            subdivisionsB = subdivisonsForB;
        }

        private static (uint, uint) GetSubdivisonCounts(int tile, int subdivisions)
        {
            var vertices = MeshExtensions.GetVertsPerFace(subdivisions);
            var indices = MeshExtensions.GetIndicesPerFace(subdivisions);
            var currentIndices = _reference.SubMeshInfos[tile].IndexCount;

            vertices *= (currentIndices / 3);
            indices *= (currentIndices / 3);

            return (vertices, indices);
        }

        private static void GetStats(DirectSubMesh[] highRes, DirectSubMesh[] lowRes)
        {
            uint Lverts = 0;
            uint Ltris = 0;
            for (int i = 0; i < lowRes[0].DirectMeshBuffer.SubMeshInfos.Length; i++)
            {
                Lverts += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].VertexCount;
                Ltris += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].IndexCount;
            }
            uint Hverts = 0;
            uint Htris = 0;
            for (int i = 0; i < highRes[0].DirectMeshBuffer.SubMeshInfos.Length; i++)
            {
                Hverts += highRes[0].DirectMeshBuffer.SubMeshInfos[i].VertexCount;
                Htris += highRes[0].DirectMeshBuffer.SubMeshInfos[i].IndexCount;
            }

            Console.WriteLine(string.Format("Low res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Lverts, Ltris, Ltris / 3));
            Console.WriteLine(string.Format("High res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Hverts, Htris, Htris / 3));
            Console.WriteLine(string.Format("Reduction rates: Verts: {0}%, Indices: {1}%", (((float)Lverts/(float)Hverts)*100f).ToString("00.00"), (((float)Ltris / (float)Htris) * 100f).ToString("00.00")));
        }

        private static void DoQuadricSimplification(DirectSubMesh[] aMeshes)
        {
            aMeshes[0].DirectMeshBuffer.ReadAllBuffers();
            _stopwatch.Restart();
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 16
            };

            float[] estimatedErrors = new float[aMeshes.Length];

            Parallel.For(0, aMeshes.Length,parallelOptions, (int i) =>
            {
                estimatedErrors[i] = Simplify(aMeshes[i]);
            });

            aMeshes[0].DirectMeshBuffer.FlushAll();
            _stopwatch.Stop();
            if (logSimplificationRMS)
            {
                Console.WriteLine();
                Console.WriteLine("Logging simplification Estiamte RMS");
                for (int i = 0; i < aMeshes.Length; i++)
                {
                    Console.WriteLine(string.Format("RMS error {0}", estimatedErrors[i]));
                }
            }
            Console.WriteLine();
            Console.WriteLine(string.Format("Simplification time: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        private static float Simplify(DirectSubMesh mesh)
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
                faceCount = (int)MathF.Floor(mesh.IndexCount / 3 * actualReductionRate)
            };
            var meshDecimation = new UnityMeshDecimation();
            meshDecimation.Execute(mesh, parameter, conditions);
            meshDecimation.ToMesh(mesh);
            return meshDecimation.EstimatedError;
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
            _deviationHeatMap = new Material("devation_heat.vert", "devation_heat.frag", typeof(ModelPushConstantData), bindingDescriptions, attributeDescriptions);

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();
            var shapeGenerator = PlanetPresets.ShapeGeneratorFixedEarthLike();
            shapeGenerator.RandomiseSettings();
            var a = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, subdivisionsA);
            var b = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, subdivisionsB);
            var c = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, subdivisionsC);
            var d = CreateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, subdivisionsD);

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(5f, 0, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(5, 0, -5f) });
            World.DefaultWorld.EntityManager.SetComponent(c, new Translation() { Value = new(-5f, 2, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(d, new Translation() { Value = new(-5f, 2, -5f) });

            _stopwatch.Restart();
            aMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager,a);
            bMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager,b);
            cMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager,c);
            dMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager,d);
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
            if (logDeviations)
            {
                for (int i = 0; i < tileIterCount; i++)
                {
                    Console.WriteLine(stats[i]);
                }
            }
            aMeshes[0].DirectMeshBuffer.FlushAll();
            bMeshes[0].DirectMeshBuffer.FlushAll();
            DirectMeshBuffer.RecalcualteAllNormals(aMeshes[0].DirectMeshBuffer);
            DirectMeshBuffer.RecalcualteAllNormals(bMeshes[0].DirectMeshBuffer);
            Console.WriteLine(string.Format("Devation Calc: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        public static DirectSubMesh[] GetMeshesInChildren(EntityManager entityManager, Entity parent)
        {
            var entityMeshes = entityManager.GetComponent<Children>(parent).Value;
            DirectSubMesh[] meshes = new DirectSubMesh[entityMeshes.Length];
            for (int j = 0; j < entityMeshes.Length; j++)
            {
                meshes[j] = DirectSubMesh.GetSubMeshAtIndex(entityManager.GetComponent<DirectSubMeshIndex>(entityMeshes[j]));
            }
            return meshes;
        }

        private static Entity CreateSubdividedPlanet(ShapeGenerator shapeGenerator,EntityManager entityManager, int subdivisons)
        {
            Entity planet = CreateUnGeneratedPlanet(entityManager, subdivisons);

            entityManager.RemoveComponentFromHierarchy<DoNotRender>(planet);
            entityManager.RemoveComponentFromHierarchy<Prefab>(planet);
            ArtifactAuthoring.GeneratePlanet(planet, shapeGenerator);

            return planet;
        }

        private static Entity CreateUnGeneratedPlanet(EntityManager entityManager, int subdivisons)
        {
            var planet = entityManager.CreateEntity();
            entityManager.AddComponent(planet, new Translation() { Value = new(0, 0f, 0) });
            entityManager.AddComponent(planet, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent<Children>(planet);
            entityManager.AddComponent(planet, new MaterialIndex { Value = Material.GetIndexOfMaterial(_deviationHeatMap) });

            ArtifactAuthoring.InitialiseTiles(entityManager, planet, subdivisons);

            var children = entityManager.GetComponent<Children>(planet);

            for (int i = 0; i < children.Value.Length; i++)
            {
                entityManager.AddComponent(children.Value[i], new MaterialIndex { Value = Material.GetIndexOfMaterial(_deviationHeatMap) });
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
