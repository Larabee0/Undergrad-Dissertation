using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using COMP302.Decimator;
using COMP302.GeoDev;
using Planets;
using Planets.Generator;
using VECS;
using VECS.DataStructures;
using VECS.ECS;
using VECS.ECS.Presentation;
using VECS.ECS.Transforms;

namespace COMP302
{
    public static class Expirment
    {

        // mesh a and mesh c are duplicates for displaying the geometric devation heatmap
        private static  int _subdivisionsA = 50; // high res
        //the actual value for this is calculated, determined by the _inputReductionRate
        private static int _subdivisionsB; // low res
        private static int _subdivisionsC = _subdivisionsA; // high res 
        private static  int _subdivisionsD = _subdivisionsA; //  simplified 
        // mesh a,b,c,d entities.
        private static Entity[] _rootEntities;


        private static float _inputReductionRate = 0.5f;
        private static float _actualReductionRate;

        private static (int, int, int) CurrentTestKey => (_seed, _subdivisionsA, (int)(_inputReductionRate * 100));

        private static Material _deviationHeatMap;


        // generation settings
        private static int _seed = -1635325477;
        private static readonly bool _randomSeed = false;

        // generation stats
        private static double _terrainGenerationTimeB;
        private static double _terrainGenerationTimeD;
        private static double _simplificationTimeD;

        // reference mesh for calculating simplification rates
        private static DirectMeshBuffer _referenceMesh;
        public static DirectMeshBuffer ReferenceMesh => _referenceMesh;
        private static readonly Stopwatch _stopwatch = new();


        public static void Run()
        {
            Init();
            if (ExpirmentConfig.TestDeviation)
            {
                TestDevation(World.DefaultWorld.EntityManager, _deviationHeatMap);
                return;
            }

            if (ExpirmentConfig.TestSimplification)
            {
                TestSimplification(World.DefaultWorld.EntityManager, _deviationHeatMap);
                return;
            }

            CreateReferencePlanet();

            RunTests();

            CSVDataHandler.ExportData();

            Console.WriteLine(string.Format("Completed runs | Elapsed time: {0}", Time.TimeSinceStartUp));
            Console.WriteLine("Press enter key to close");
            Console.ReadLine();
            Application.Exit();
        }

        public static void Init()
        {
            VertexAttributeDescription[] _vertexAttributeDescriptions =
            [
                new(VertexAttribute.Position,VertexAttributeFormat.Float3,0,0,0),
                new(VertexAttribute.Normal,VertexAttributeFormat.Float3,0,1,1),
                new(VertexAttribute.TexCoord0,VertexAttributeFormat.Float2,0,2,2),
            ];

            var bindingDescriptions = DirectMeshBuffer.GetBindingDescription(_vertexAttributeDescriptions);
            var attributeDescriptions = DirectMeshBuffer.GetAttributeDescriptions(_vertexAttributeDescriptions);
            _deviationHeatMap = new Material(
                "devation_heat.vert",
                "devation_heat.frag",
                typeof(ModelPushConstantData),
                bindingDescriptions,
                attributeDescriptions);

            World.DefaultWorld.CreateSystem<TexturelessRenderSystem>();
            World.DefaultWorld.CreateSystem<RotatorSystem>();
        }

        public static void CreateReferencePlanet()
        {
            Entity root = CreateSubdividedPlanet(World.DefaultWorld.EntityManager, 0);
            DirectSubMeshIndex[] submehses = World.DefaultWorld.EntityManager.GetComponentsInHierarchy<DirectSubMeshIndex>(root);
            // all mesehs within a planet share the same direct mesh buffer
            _referenceMesh = DirectMeshBuffer.GetMeshAtIndex(submehses[0].DirectMeshBuffer);
        }

        private static void RunTests()
        {
            var subDivLevels = ExpirmentConfig.SubdivisonLevels;
            var reductRates = ExpirmentConfig.SimplificationRates;
            for (int s = 0; s < subDivLevels.Length; s++)
            {

                _subdivisionsA = subDivLevels[s];
                // _subdivisionsB is calculated internally
                _subdivisionsC = subDivLevels[s];
                _subdivisionsD = subDivLevels[s];
                var upperProgress = string.Format("Subdivision: {0}/{1}", s + 1, subDivLevels.Length);
                for (int r = 0; r < reductRates.Length; r++)
                {
                    var innerProgress = string.Format("Simplification Rate: {0}/{1}", r + 1, reductRates.Length);
                    _inputReductionRate = reductRates[r];
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        var planetProgress = string.Format("Planet: {0}/{1}", i + 1, ExpirmentConfig.PlanetCount);
                        Console.WriteLine(string.Format("{0}\nof {1}\nof {2}\nElapsed Time: {3}", planetProgress, innerProgress, upperProgress, Time.TimeSinceStartUpAsDouble.ToString("000000s")));
                        _seed = ExpirmentConfig.Seeds[i];
                        RunOnce();
                    }
                }
            }
        }

        public static void RunOnce()
        {
            CleanUp();
            _inputReductionRate = Math.Clamp(_inputReductionRate, 0, 1);
            _subdivisionsB = ExpirmentUtilities.GetClosestTerrainGenSimplificationRate(_subdivisionsA, _inputReductionRate, out _actualReductionRate);
            DoGeneration(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes);
            Console.WriteLine(string.Format("Begin Test| Sub: {0} In Reduct: {1} Seed: {2}", _subdivisionsA, (_inputReductionRate * 100f).ToString("000"), _seed));
            GC.Collect();
            if (ExpirmentConfig.EnableQuadricSimplification)
            {
                DoQuadricSimplification(dMeshes);
                GC.Collect();
                CSVDataHandler.AddExeuctionData(CurrentTestKey, _terrainGenerationTimeB, _terrainGenerationTimeD, _simplificationTimeD, aMeshes, bMeshes, cMeshes, dMeshes);
            }
            


            if (ExpirmentConfig.EnableDevation)
            {
                Console.WriteLine();
                Console.WriteLine("Calculating Meshes B Deviations (High Res vs Low Res Generation)");
                float maxDev = float.MinValue;
                maxDev = MathF.Max(DoDeviation(0, aMeshes, bMeshes),maxDev);

                GC.Collect();

                Console.WriteLine();
                Console.WriteLine("Calculating Meshes D Deviations (High Res vs Quadric Simplified)");
                maxDev = MathF.Max(DoDeviation(1, cMeshes, dMeshes), maxDev);
                ExpirmentUtilities.SetUV_Y(maxDev,aMeshes,bMeshes,cMeshes,dMeshes);
                GC.Collect();
            }

            Console.WriteLine(string.Format("\nInput reduct-rate: {0}% | Actual: {1}% | Subdivisions (Mesh B) {2}", (_inputReductionRate * 100f).ToString("00.00"), (_actualReductionRate * 100f).ToString("00.00"), _subdivisionsB));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Meshes A-B (High Res vs Low Res Generation");
            ExpirmentUtilities.GetStats(aMeshes, bMeshes);
            Console.WriteLine();
            Console.WriteLine("Meshes C-D (High Res vs Quadric Simplified)");
            ExpirmentUtilities.GetStats(cMeshes, dMeshes);
            Console.WriteLine();
            ExpirmentUtilities.CalculateVertsAndTris(aMeshes, out uint Hverts, out uint Htris);
            Console.WriteLine(string.Format("Seed: {0}, Src Vert Count: {1} Src Tri Count: {2}", _seed, Hverts, Htris / 3));
        }

        private static void DoGeneration(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes)
        {
            ShapeGenerator shapeGenerator = PlanetPresets.ShapeGeneratorFixedEarthLike();
            if (_randomSeed)
            {
                _seed = shapeGenerator.RandomiseSeed();
            }
            else
            {
                shapeGenerator.SetSeed(_seed);
            }

            Entity a = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsA, out aMeshes);
            Entity b = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsB, out bMeshes);
            _terrainGenerationTimeB = _stopwatch.Elapsed.TotalMilliseconds;
            Entity c = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsC, out cMeshes);
            Entity d = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsD, out dMeshes);
            _terrainGenerationTimeD = _stopwatch.Elapsed.TotalMilliseconds;

            _rootEntities = [a, b, c, d];

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(5f, 0, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(5, 0, -5f) });
            World.DefaultWorld.EntityManager.SetComponent(c, new Translation() { Value = new(-5f, 2, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(d, new Translation() { Value = new(-5f, 2, -5f) });

            _stopwatch.Restart();

            DirectMeshBuffer.ReadAllBuffersBatched(
                aMeshes[0].DirectMeshBuffer,
                bMeshes[0].DirectMeshBuffer,
                cMeshes[0].DirectMeshBuffer,
                dMeshes[0].DirectMeshBuffer);
            _stopwatch.Stop();
            Console.WriteLine(string.Format("Copy back time: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        private static void DoQuadricSimplification(DirectSubMesh[] aMeshes)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 16
            };

            _stopwatch.Restart();
            Parallel.For(0, aMeshes.Length, parallelOptions, (int i) =>
            {
                Simplify(aMeshes[i]);
            });

            aMeshes[0].DirectMeshBuffer.FlushAll();
            _stopwatch.Stop();
            _simplificationTimeD = _stopwatch.Elapsed.TotalMilliseconds;
            Console.WriteLine();
            Console.WriteLine(string.Format("Simplification time: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));
        }

        public static void Simplify(DirectSubMesh mesh, TargetConditions conditions = null)
        {
            var parameter = new EdgeCollapseParameter
            {
                UsedProperty = VertexProperty.UV0,
                PreserveBoundary = true,
                NormalCheck = false,
                BoundaryWeight = 0.5f
            };
            conditions ??= new TargetConditions
            {
                faceCount = (int)MathF.Floor(mesh.IndexCount / 3 * _actualReductionRate)
            };
            var meshDecimation = new UnityMeshDecimation();
            meshDecimation.Execute(mesh, parameter, conditions);
            meshDecimation.ToMesh(mesh);
        }

        public static float DoDeviation(int method, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes)
        {
            _stopwatch.Restart();
            
            string[] stats = new string[aMeshes.Length];
            Vector3 elevationMeans = new(float.MaxValue, float.MinValue, 0);
            Vector3 meanDevStats = new(float.MaxValue, float.MinValue, 0);

            aMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            bMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            float maxDev = float.MinValue;
            for (int i = 0; i < aMeshes.Length; i++)
            {
                RunSingleDeviation(method, aMeshes, bMeshes, stats, ref elevationMeans, ref meanDevStats, ref maxDev, i);
            }

            elevationMeans.Z /= aMeshes.Length;
            meanDevStats.Z /= aMeshes.Length;

            _stopwatch.Stop();

            Console.WriteLine(string.Format("Devation Calc: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));

            CSVDataHandler.AddDeviationData(CurrentTestKey, method, stats, aMeshes, bMeshes, elevationMeans, meanDevStats);

            return maxDev;
        }

        private static void RunSingleDeviation(int method, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, string[] stats, ref Vector3 elevationMeans, ref Vector3 meanDevStats, ref float maxDev, int i)
        {
            Span<Vector2> buvs = bMeshes[i].GetVertexDataSpan<Vector2>(VertexAttribute.TexCoord0);
            Span<Vector2> auvs = aMeshes[i].GetVertexDataSpan<Vector2>(VertexAttribute.TexCoord0);
            float minElevation = float.MaxValue;
            float maxElevation = float.MinValue;
            float meanElevation = 0;

            for (int j = 0; j < aMeshes[i].VertexCount; j++)
            {
                minElevation = MathF.Min(minElevation, auvs[j].X);
                maxElevation = MathF.Max(maxElevation, auvs[j].X);
                meanElevation += auvs[j].X;
            }
            meanElevation /= aMeshes[i].VertexCount;

            elevationMeans.X = MathF.Min(minElevation, elevationMeans.X);
            elevationMeans.Y = MathF.Max(maxElevation, elevationMeans.Y);
            elevationMeans.Z += meanElevation;

            for (int j = 0; j < bMeshes[i].VertexCount; j++)
            {
                buvs[j].X = 0;
            }


            var deviation = new Deviation();

            deviation.Initialization(aMeshes[i], bMeshes[i]);
            deviation.Compute(ExpirmentConfig.NormalDevation, ExpirmentConfig.ParallelDevation);

            uint bverts = bMeshes[i].VertexCount;
            uint btris = bMeshes[i].IndexCount;
            btris /= 3;
            Vector2 actualReductionRates = ExpirmentUtilities.CalculateResultantSimplificationRates(aMeshes[i], bMeshes[i]);
            stats[i] = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}",
                _seed,
                i,
                method,
                _subdivisionsA,
                (int)(_inputReductionRate * 100),
                bverts,
                btris,
                actualReductionRates.X,
                actualReductionRates.Y,
                minElevation,
                maxElevation,
                meanElevation,
                deviation.GetCSVStatisticRow());

            var devData = deviation.GetDataForMean();

            meanDevStats.X = MathF.Min(devData.X, meanDevStats.X);
            meanDevStats.Y = MathF.Max(devData.Y, meanDevStats.Y);
            meanDevStats.Z += devData.Z;

            maxDev = MathF.Max(deviation.DevBound, maxDev);

            if (ExpirmentConfig.TestDeviation)
            {
                Console.WriteLine(deviation.GetStatisticsString());
            }
        }

        public static void VisualiseResult()
        {
            VisualiserSetSeed();
            VisualiserSetSubdivision();
            VisualiserSetReductionRate();

            Init();
            CreateReferencePlanet();
            RunOnce();
        }

        private static void VisualiserSetSeed()
        {
            Console.WriteLine("Choose test to visualise\n\nPick Planet number");

            for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
            {
                Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, ExpirmentConfig.Seeds[i]));
            }
            string planetIndexInput = Console.ReadLine();
            int planetIndex;
            while (!int.TryParse(planetIndexInput, out planetIndex) || (planetIndex - 1 < 0 || planetIndex - 1 >= ExpirmentConfig.PlanetCount))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", planetIndexInput);
                for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                {
                    Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, ExpirmentConfig.Seeds[i]));
                }
                planetIndexInput = Console.ReadLine();
            }
            _seed = ExpirmentConfig.Seeds[planetIndex - 1];
        }

        private static void VisualiserSetSubdivision()
        {
            Console.WriteLine("Seed set to: {0}\n\nPick Subdivision Level", _seed);

            for (int i = 0; i < ExpirmentConfig.SubdivisonLevels.Length; i++)
            {
                Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, ExpirmentConfig.SubdivisonLevels[i]));
            }
            string subdivisionIndexInput = Console.ReadLine();
            int subdivisionIndex;
            while (!int.TryParse(subdivisionIndexInput, out subdivisionIndex) || (subdivisionIndex - 1 < 0 || subdivisionIndex - 1 >= ExpirmentConfig.SubdivisonLevels.Length))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", subdivisionIndex);
                for (int i = 0; i < ExpirmentConfig.SubdivisonLevels.Length; i++)
                {
                    Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, ExpirmentConfig.SubdivisonLevels[i]));
                }
                subdivisionIndexInput = Console.ReadLine();
            }

            _subdivisionsA = ExpirmentConfig.SubdivisonLevels[subdivisionIndex - 1];
            _subdivisionsC = ExpirmentConfig.SubdivisonLevels[subdivisionIndex - 1];
            _subdivisionsD = ExpirmentConfig.SubdivisonLevels[subdivisionIndex - 1];
            Console.WriteLine("Subdivision level set to: {0}\n\nPick Reduction rate", _subdivisionsA);
        }

        private static void VisualiserSetReductionRate()
        {
            for (int i = 0; i < ExpirmentConfig.SimplificationRates.Length; i++)
            {
                Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (ExpirmentConfig.SimplificationRates[i] * 100).ToString("00.00")));
            }
            string reductionRateIndexInput = Console.ReadLine();
            int reductionRateIndex;
            while (!int.TryParse(reductionRateIndexInput, out reductionRateIndex) || (reductionRateIndex - 1 < 0 || reductionRateIndex - 1 >= ExpirmentConfig.SimplificationRates.Length))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", reductionRateIndex);
                for (int i = 0; i < ExpirmentConfig.SimplificationRates.Length; i++)
                {
                    Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (ExpirmentConfig.SimplificationRates[i] * 100).ToString("00.00")));
                }
                reductionRateIndexInput = Console.ReadLine();
            }

            _inputReductionRate = ExpirmentConfig.SimplificationRates[reductionRateIndex - 1];
            Console.WriteLine("Reduction rate level set to: {0}%\n\nComputing Result", (_inputReductionRate * 100).ToString("00.00"));
        }

        private static void CleanUp()
        {
            var entityManager = World.DefaultWorld.EntityManager;
            if (_rootEntities != null)
            {
                for (int i = 0; i < _rootEntities.Length; i++)
                {
                    DirectSubMeshIndex[] meshes = entityManager.GetComponentsInHierarchy<DirectSubMeshIndex>(_rootEntities[i]);

                    for (int j = 0; j < meshes.Length; j++)
                    {
                        DirectMeshBuffer.GetMeshAtIndex(meshes[i].DirectMeshBuffer)?.Dispose();
                    }

                    entityManager.DestroyEntityHierarchy(_rootEntities[i]);
                }

                _rootEntities = null;
            }
            GC.Collect();
        }

        private static Entity GenerateSubdividedPlanet(ShapeGenerator shapeGenerator,EntityManager entityManager, int subdivisons, out DirectSubMesh[] meshes)
        {
            Entity planet = CreateSubdividedPlanet(entityManager, subdivisons);

            entityManager.RemoveComponentFromHierarchy<DoNotRender>(planet);
            entityManager.RemoveComponentFromHierarchy<Prefab>(planet);
            entityManager.AddComponent<RotateObject>(planet);
            entityManager.AddComponent<Rotation>(planet);
            _stopwatch.Restart();
            ArtifactAuthoring.GeneratePlanet(planet, shapeGenerator);
            _stopwatch.Stop();
            meshes = ExpirmentUtilities.GetMeshesInChildren(World.DefaultWorld.EntityManager, planet);
            return planet;
        }

        private static Entity CreateSubdividedPlanet(EntityManager entityManager, int subdivisons)
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


        public static void TestDevation(EntityManager entityManager, Material mat)
        {
            var sphere = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("Sphere.obj"), null);
            var cube = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("cube-UV.obj"), null);

            var sphereEntity = entityManager.CreateEntity();
            var cubeEntity = entityManager.CreateEntity();


            entityManager.AddComponent(sphereEntity, sphere[0].GetSubMeshIndex());
            entityManager.AddComponent(sphereEntity, new Translation() { Value = new(-4, 0f, 0) });
            entityManager.AddComponent(sphereEntity, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent(sphereEntity, new MaterialIndex { Value = Material.GetIndexOfMaterial(mat) });

            entityManager.AddComponent(cubeEntity, cube[0].GetSubMeshIndex());
            entityManager.AddComponent(cubeEntity, new Translation() { Value = new(4, 0f, 0) });
            entityManager.AddComponent(cubeEntity, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent(cubeEntity, new MaterialIndex { Value = Material.GetIndexOfMaterial(mat) });

            float maxDev = DoDeviation(0, sphere, cube);

            ExpirmentUtilities.SetUV_Ys(maxDev, sphere[0].DirectMeshBuffer);
            ExpirmentUtilities.SetUV_Ys(maxDev, cube[0].DirectMeshBuffer);
        }

        public static void TestSimplification(EntityManager entityManager, Material mat)
        {
            ExpirmentConfig.TestDeviation = true;
            VertexAttributeDescription[] additionalAttributes = [new VertexAttributeDescription(VertexAttribute.TexCoord0, VertexAttributeFormat.Float2)];
            var rawBunny = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("StanfordBunny.obj"), additionalAttributes);
            var simplifiedBunny = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("StanfordBunny.obj"), additionalAttributes);
            var rawBun = entityManager.CreateEntity();
            var simpBun = entityManager.CreateEntity();


            entityManager.AddComponent(rawBun, rawBunny[0].GetSubMeshIndex());
            entityManager.AddComponent(rawBun, new Translation() { Value = new(5, 0f, 0) });
            entityManager.AddComponent(rawBun, new Scale() { Value = new(30f, 30f, 30f) });
            entityManager.AddComponent(rawBun, new Rotation() { Value = new(0f, 180f, 0f) });
            entityManager.AddComponent(rawBun, new MaterialIndex { Value = Material.GetIndexOfMaterial(mat) });
            entityManager.AddComponent<RotateObject>(rawBun);

            entityManager.AddComponent(simpBun, simplifiedBunny[0].GetSubMeshIndex());
            entityManager.AddComponent(simpBun, new Translation() { Value = new(-5, 0f, 0) });
            entityManager.AddComponent(simpBun, new Scale() { Value = new(30f, 30f, 30f) });
            entityManager.AddComponent(simpBun, new Rotation() { Value = new(0f, 180f, 0f) });
            entityManager.AddComponent(simpBun, new MaterialIndex { Value = Material.GetIndexOfMaterial(mat) });
            entityManager.AddComponent<RotateObject>(simpBun);
            Console.WriteLine("Performing Simplification");
            Simplify(simplifiedBunny[0], new TargetConditions() { faceCount = 1000 });
            Console.WriteLine("Tris {0}, Indices: {1}", simplifiedBunny[0].IndexCount / 3, simplifiedBunny[0].IndexCount);
            Console.WriteLine("Simplification Finished");

            Console.WriteLine("Performing Geometric Devation");
            float maxDev = DoDeviation(0, rawBunny, simplifiedBunny);
            Console.WriteLine("Geometric Devation Finished");


            ExpirmentUtilities.SetUV_Ys(maxDev, rawBunny[0].DirectMeshBuffer);
            ExpirmentUtilities.SetUV_Ys(maxDev, simplifiedBunny[0].DirectMeshBuffer);
        }
    }
}
