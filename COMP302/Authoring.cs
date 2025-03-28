using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        // data collection
        private static string _resultOutputPath = "Results/Test-13";
        private static readonly string[] _csvHeaderDev = ["Seed, Tile_ID, Algorithm, Src_SubDiv, Input_Reduction, Vert_Count, Tri_Count, Vert_Reduction, Tri_Reduction, Min_Elev, Max_Elev, Mean_Elev, Min_Dev, Max_Dev, Mean_Dev"];
        private static readonly string[] _csvHeaderExeTime = ["Seed, Tile_ID, Algorithm, Src_SubDiv, Input_Reduction, Vert_Count, Tri_Count, Vert_Reduction, Tri_Reduction, Execution_Time"];

        // algorithim = 0 = terrain generator
        // algorithim = 1 = Quadric Simplification

        private static string[] _simpTerrainGenCSV = _csvHeaderDev;
        private static string[] _simpTerrainGenSummaryCSV = _csvHeaderDev;

        private static string[] _subTerrainGenCSV = _csvHeaderDev;
        private static string[] _subTerrainGenSummaryCSV = _csvHeaderDev;

        private static string[] _simpQuadricSimplificationCSV = _csvHeaderDev;
        private static string[] _simpQuadricSimplificationSummaryCSV = _csvHeaderDev;

        private static string[] _subQuadricSimplificationCSV = _csvHeaderDev;
        private static string[] _subQuadricSimplificationSummaryCSV = _csvHeaderDev;


        private static string[] _exeTimeSimpBySubTerrainGenCSV = _csvHeaderExeTime;
        private static string[] _exeTimeSimpBySubQuadricSimplificationCSV = _csvHeaderExeTime;

        private static string[] _exeTimeSubBySimpTerrainGenCSV= _csvHeaderExeTime;
        private static string[] _exeTimeSubBySimpQuadricSimplificationCSV= _csvHeaderExeTime;


        private static readonly Dictionary<(int, int, int), string[]> _testsTerrainGen = [];
        private static readonly Dictionary<(int, int, int), string[]> _testsQuadricSimplification = [];

        private static readonly Dictionary<(int, int, int), string> _summaryTestsTerrainGen = [];
        private static readonly Dictionary<(int, int, int), string> _summaryTestsQuadricSimplification = [];

        private static readonly Dictionary<(int, int, int), string> _executionTimeTerrainGen = [];
        private static readonly Dictionary<(int, int, int), string> _executionTimeQuadricSimplification = [];

        // start mesh subdivisions.
        private static readonly bool _runAllSubdivisions = true;
        private static int[] _subdivisonLevels = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];
        //private static readonly int[] _subdivisonLevels = [20];

        // mesh a and mesh c are duplicates for displaying the geometric devation heatmap
        private static  int _subdivisionsA = 50; // high res
        //the actual value for this is calculated, determined by the _inputReductionRate
        private static int _subdivisionsB; // low res
        private static int _subdivisionsC = _subdivisionsA; // high res 
        private static  int _subdivisionsD = _subdivisionsA; //  simplified 
        // mesh a,b,c,d entities.
        private static Entity[] _rootEntities;

        // simplification settings
        private static readonly bool _enableQuadricSimplification = true;
        private static readonly bool _logSimplificationRMS = false;
        private static float _inputReductionRate = 0.5f;
        private static float _actualReductionRate;
        private static readonly bool _runAllReductionRates = true;
        private static float[] _simplificationRates = [0.95f, 0.90f, 0.85f, 0.80f, 0.75f, 0.70f, 0.65f, 0.60f, 0.55f, 0.50f, 0.45f, 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, 0.05f];
        //private static readonly float[] _simplificationRates = [0.5f];

        private static (int, int, int) CurrentTestKey => (_seed, _subdivisionsA, (int)(_inputReductionRate * 100));

        // geometric devation settings
        private static Material _deviationHeatMap;
        private static readonly bool _enableDevation = true;
        private static readonly bool _normalDevation = false;
        private static readonly bool _parallelDevation = true;
        private static readonly bool _logDeviations = false;
        private static readonly bool _logExecutionTime = false;
        private static readonly bool _testDeviation = false;

        private static int[] _seeds = [
            -238246973,
            -1981713786,
            -728742154,
            1884866878,
            -772070360,
            -2085966360,
            1172812092,
            961934364,
            1361839499,
            1293553328
        ];

        private static int _planetCount = 10; // set this to 10
        // planet iteration settings (basically, do we look at each tile on a planet
        private static int _tileIterCount = 10;
        private static readonly bool _interAllTiles = true;

        // generation settings
        private static int _seed = -1635325477;
        private static readonly bool _randomSeed = false;

        // generation stats
        private static double _terrainGenerationTimeB;
        private static double _terrainGenerationTimeD;
        private static double _simplificationTimeD;

        // reference mesh for calculating simplification rates
        private static DirectMeshBuffer _reference;
        
        private static readonly Stopwatch _stopwatch = new();


        public static void MainMenu()
        {
            Console.WriteLine("Select option");
            Console.WriteLine("00. Exit");
            Console.WriteLine("01. View current Seeds");
            Console.WriteLine("02. Generate Seeds");
            Console.WriteLine("03. Set Seeds");
            Console.WriteLine("04. View Subdivision Steps");
            Console.WriteLine("05. Set Subdivision Steps");
            Console.WriteLine("06. View Input reduction levels");
            Console.WriteLine("07. Set Input reduction levels");
            Console.WriteLine("08. Visualise result");
            Console.WriteLine("09. Run all tests as configured");
            Console.WriteLine("10. View output directory");
            Console.WriteLine("11. Set output directory");
            string input = Console.ReadLine();
            if(!int.TryParse(input,out int results))
            {
                InvalidInput(input, MainMenu);
            }
            switch(results)
            {
                case 0:
                    Application.Exit();
                    return;
                case 1:
                    for (int i = 0; i < _planetCount; i++)
                    {
                        Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, _seeds[i]));
                    }
                    break;
                case 2:
                    GenerateSeedsInput();
                    break;
                case 3:
                    SetSeeds();
                    break;
                case 4:
                    for (int i = 0; i < _subdivisonLevels.Length; i++)
                    {
                        Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, _subdivisonLevels[i]));
                    }
                    break;
                case 5:
                    SetSubdivisionLevels();
                    break;
                case 6:
                    for (int i = 0; i < _simplificationRates.Length; i++)
                    {
                        Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (_simplificationRates[i]*100).ToString("00.00")));
                    }
                    break;
                case 7:
                    SetInputReductionRates();
                    break;
                case 8:
                    VisualiseResult();
                    return;
                case 9:
                    Run();
                    return;
                case 10:
                    Console.WriteLine(_resultOutputPath);
                    break;
                case 11:
                    SetOutputDirectory();
                    break;
                default:
                    InvalidInput(results.ToString(), null);
                    break;
            }
            Console.WriteLine("\n\n");
            MainMenu();
        }

        private static void VisualiseResult()
        {
            Console.WriteLine("Choose test to visualise\n\nPick Planet number");

            for (int i = 0; i < _planetCount; i++)
            {
                Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, _seeds[i]));
            }
            string planetIndexInput = Console.ReadLine();
            int planetIndex;
            while (!int.TryParse(planetIndexInput, out planetIndex)||( planetIndex -1 < 0 || planetIndex-1 >= _planetCount))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", planetIndexInput);
                for (int i = 0; i < _planetCount; i++)
                {
                    Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, _seeds[i]));
                }
                planetIndexInput = Console.ReadLine();
            }
            _seed = _seeds[planetIndex-1];


            Console.WriteLine("Seed set to: {0}\n\nPick Subdivision Level", _seed);

            for (int i = 0; i < _subdivisonLevels.Length; i++)
            {
                Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, _subdivisonLevels[i]));
            }
            string subdivisionIndexInput = Console.ReadLine();
            int subdivisionIndex;
            while (!int.TryParse(subdivisionIndexInput, out subdivisionIndex) || (subdivisionIndex - 1 < 0 || subdivisionIndex - 1>= _subdivisonLevels.Length))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", subdivisionIndex);
                for (int i = 0; i < _subdivisonLevels.Length; i++)
                {
                    Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, _subdivisonLevels[i]));
                }
                subdivisionIndexInput = Console.ReadLine();
            }

            _subdivisionsA = _subdivisonLevels[subdivisionIndex - 1];
            _subdivisionsC = _subdivisonLevels[subdivisionIndex - 1];
            _subdivisionsD = _subdivisonLevels[subdivisionIndex - 1];
            Console.WriteLine("Subdivision level set to: {0}\n\nPick Reduction rate", _subdivisionsA);

            for (int i = 0; i < _simplificationRates.Length; i++)
            {
                Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (_simplificationRates[i] * 100).ToString("00.00")));
            }
            string reductionRateIndexInput = Console.ReadLine();
            int reductionRateIndex;
            while (!int.TryParse(reductionRateIndexInput, out reductionRateIndex) || (reductionRateIndex - 1 < 0 || reductionRateIndex - 1 >= _simplificationRates.Length))
            {
                Console.WriteLine("Invalid input or index out of range: {0}", reductionRateIndex);
                for (int i = 0; i < _simplificationRates.Length; i++)
                {
                    Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (_simplificationRates[i] * 100).ToString("00.00")));
                }
                reductionRateIndexInput = Console.ReadLine();
            }

            _inputReductionRate = _simplificationRates[reductionRateIndex-1];
            Console.WriteLine("Reduction rate level set to: {0}%\n\nComputing Result", (_inputReductionRate*100).ToString("00.00"));

            Init();
            CreateReferencePlanet();
            RunOnce();
        }

        private static void SetOutputDirectory()
        {
            Console.WriteLine("Set out local output directory");
            string newDirectory = Console.ReadLine();
            if (Uri.IsWellFormedUriString(newDirectory, UriKind.Relative))
            {
                _resultOutputPath = newDirectory;
                Console.WriteLine("Directory set.\nFull output path: {0}", Path.Combine(Application.ExecutingDirectory, _resultOutputPath));
            }
            else
            {
                InvalidInput(newDirectory, SetOutputDirectory);
            }
        }

        private static void SetInputReductionRates()
        {
            Console.WriteLine(string.Format("\n\nCurrent number of Reduction levels: {0}", _simplificationRates.Length));
            Console.WriteLine(string.Format("Enter number of reduction levels\n"));
            string reductionCountText = Console.ReadLine();
            if (int.TryParse(reductionCountText, out int result))
            {
                if (result < 1)
                {
                    Console.WriteLine(string.Format("\n\nValue out of range Invalid \"{0}\"\nValue must be greater or equal to 1.", result));
                    SetInputReductionRates();
                }
                _simplificationRates = new float[result];
            }
            else
            {
                InvalidInput(reductionCountText, SetInputReductionRates);
                return;
            }


            for (int i = 0; i < _simplificationRates.Length; i++)
            {
                Console.WriteLine("Enter reduction rate for step: {0}, this should be a fractional value (0-1) where 0.5 => 50%", i + 1);
                string simplificationRate = Console.ReadLine();
                if (float.TryParse(simplificationRate, out float reductionRate) && reductionRate > 0)
                {
                    _simplificationRates[i] = reductionRate;
                    Console.WriteLine("Reduction rate level: {0} Set to: {1}%\n\n", i + 1, (_simplificationRates[i] * 100).ToString("00.00"));
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}, must be >= 0 and <= 1", simplificationRate);
                    i--;
                }
            }
            Console.WriteLine("All Reduction rates Set\n\n");
        }

        private static void SetSubdivisionLevels()
        {
            Console.WriteLine(string.Format("\n\nCurrent number of subdivision steps: {0}", _subdivisonLevels.Length));
            Console.WriteLine(string.Format("Enter subdivision count\n"));
            string subdivisionCountText = Console.ReadLine();
            if (int.TryParse(subdivisionCountText, out int result))
            {
                if (result < 1)
                {
                    Console.WriteLine(string.Format("\n\nValue out of range Invalid \"{0}\"\nValue must be greater or equal to 1.", result));
                    SetSubdivisionLevels();
                }
                _subdivisonLevels = new int[result];
            }
            else
            {
                InvalidInput(subdivisionCountText, SetSubdivisionLevels);
                return;
            }

            for (int i = 0; i < _subdivisonLevels.Length; i++)
            {
                Console.WriteLine("Enter subidivions for step: {0}", i + 1);
                string subdivisionInput = Console.ReadLine();
                if (int.TryParse(subdivisionInput, out int divisions) && divisions > 0)
                {
                    _subdivisonLevels[i] = divisions;
                    Console.WriteLine("Subdivision Step: {0} Set to: {1}\n\n", i + 1, divisions);
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}, must be greater than 0", subdivisionInput);
                    i--;
                }
            }
            Console.WriteLine("All subdivisions Set\n\n");
            MainMenu();
        }

        private static void SetSeeds()
        {
            SetPlanetCount(EnterSeeds);
        }

        private static void EnterSeeds(int planetCount)
        {
            _planetCount = planetCount;
            Console.WriteLine("Set planet count to: {0}", _planetCount);
            _seeds = new int[_planetCount];
            for (int i = 0; i < _planetCount; i++)
            {
                Console.WriteLine("Enter seed for planet: {0}", i + 1);
                string seedInput = Console.ReadLine();
                if(int.TryParse(seedInput,out int seed))
                {
                    _seeds[i] = seed;
                    Console.WriteLine("Planet: {0} Seed Set to: {1}\n\n",i+1,seed);
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}", seedInput);
                    i--;
                }
            }
            Console.WriteLine("All Seeds Set\n\n");
            MainMenu();
        }

        private static void GenerateSeedsInput()
        {
            SetPlanetCount(SetAndGeneratePlanetSeeds);
        }

        private static void SetPlanetCount(Action<int> successCallback)
        {
            Console.WriteLine(string.Format("\n\nCurrent planet count: {0}", _planetCount));
            Console.WriteLine(string.Format("Enter planet count\n"));
            string plantCountText = Console.ReadLine();
            if (int.TryParse(plantCountText, out int result))
            {
                if (result < 1)
                {
                    Console.WriteLine(string.Format("\n\nValue out of range Invalid \"{0}\"\nValue must be greater or equal to 1.", result));
                    GenerateSeedsInput();
                }
                successCallback?.Invoke(result);
            }
            else
            {
                InvalidInput(plantCountText, GenerateSeedsInput);
            }
        }

        private static void SetAndGeneratePlanetSeeds(int plants)
        {
            _planetCount = plants;
            Console.WriteLine(string.Format("Set planet count to: {0}", _planetCount));
            GenerateSeeds();
            Console.WriteLine("Generated Seeds Set\n\n");
            MainMenu();
        }

        private static void InvalidInput(string input, Action callback)
        {
            Console.WriteLine(string.Format("Invalid input: \"{0}\"", input));
            callback?.Invoke();
        }


        public static void Run()
        {
            Init();
            if (_testDeviation)
            {
                TestDevation(World.DefaultWorld.EntityManager);
                return;
            }


            CreateReferencePlanet();

            RunTests();

            string outputPath = Path.Combine(Application.ExecutingDirectory, _resultOutputPath);
            Directory.CreateDirectory(outputPath);
            if (_logDeviations)
            {

                CreatedCSVOrderedBySimplificationThenSubdivisions();
                CreatedCSVOrderedBySubdivisionsThenSimplification();

                ExportDeviationCSVs(outputPath);
            }

            if (_logExecutionTime)
            {
                Created_EXE_TIME_CSVOrderedBySimplificationThenSubdivisions();
                Created_EXE_TIME_CSVOrderedBySubdivisionsThenSimplification();

                ExportExecutionTimeCSV(outputPath);
            }

            Console.WriteLine(string.Format("Completed runs | Elapsed time: {0}", Time.TimeSinceStartUp));
            Console.WriteLine("Press enter key to close");
            Console.ReadLine();
            Application.Exit();
        }

        private static void GenerateSeeds()
        {
            _seeds = new int[_planetCount];
            for (int i = 0; i < _planetCount; i++)
            {
                _seeds[i] = Random.Shared.Next(int.MinValue, int.MaxValue);
            }
        }

        private static void ExportExecutionTimeCSV(string outputPath)
        {
            File.WriteAllLines(Path.Combine(outputPath, "EXE_SUB_Terrain_Generator.csv"), _exeTimeSubBySimpTerrainGenCSV);
            File.WriteAllLines(Path.Combine(outputPath, "EXE_SIMP_Terrain_Generator.csv"), _exeTimeSimpBySubTerrainGenCSV);

            File.WriteAllLines(Path.Combine(outputPath, "EXE_SUB_Quadric_Simplification.csv"), _exeTimeSubBySimpQuadricSimplificationCSV);
            File.WriteAllLines(Path.Combine(outputPath, "EXE_SIMP_Quadric_Simplification.csv"), _exeTimeSimpBySubQuadricSimplificationCSV);
        }

        private static void Created_EXE_TIME_CSVOrderedBySimplificationThenSubdivisions()
        {
            for (int s = 0; s < _subdivisonLevels.Length; s++)
            {
                for (int r = 0; r < _simplificationRates.Length; r++)
                {
                    for (int i = 0; i < _planetCount; i++)
                    {
                        var key = (_seeds[i], _subdivisonLevels[s], (int)(_simplificationRates[r] * 100));

                        var terrainGen = _executionTimeTerrainGen[key];
                        var quadSimp = _executionTimeQuadricSimplification[key];

                        _exeTimeSimpBySubTerrainGenCSV = [.. _exeTimeSimpBySubTerrainGenCSV, terrainGen];
                        _exeTimeSimpBySubQuadricSimplificationCSV = [.. _exeTimeSimpBySubQuadricSimplificationCSV, quadSimp];
                    }
                }
            }
        }

        private static void Created_EXE_TIME_CSVOrderedBySubdivisionsThenSimplification()
        {
            for (int r = 0; r < _simplificationRates.Length; r++)
            {
                for (int s = 0; s < _subdivisonLevels.Length; s++)
                {
                    for (int i = 0; i < _planetCount; i++)
                    {
                        var key = (_seeds[i], _subdivisonLevels[s], (int)(_simplificationRates[r] * 100));

                        var terrainGen = _executionTimeTerrainGen[key];
                        var quadSimp = _executionTimeQuadricSimplification[key];

                        _exeTimeSubBySimpTerrainGenCSV = [.. _exeTimeSubBySimpTerrainGenCSV, terrainGen];
                        _exeTimeSubBySimpQuadricSimplificationCSV = [.. _exeTimeSubBySimpQuadricSimplificationCSV, quadSimp];
                    }
                }
            }
        }
        private static void ExportDeviationCSVs(string outputPath)
        {
            File.WriteAllLines(Path.Combine(outputPath, "SUB_Terrain_Generator.csv"), _subTerrainGenCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SUMMARY_SUB_Terrain_Generator.csv"), _subTerrainGenSummaryCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SIMP_Terrain_Generator.csv"), _simpTerrainGenCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SUMMARY_SIMP_Terrain_Generator.csv"), _simpTerrainGenSummaryCSV);

            File.WriteAllLines(Path.Combine(outputPath, "SUB_Quadric_Simplification.csv"), _subQuadricSimplificationCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SUMMARY_SUB_Quadric_Simplification.csv"), _subQuadricSimplificationSummaryCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SIMP_Quadric_Simplification.csv"), _simpQuadricSimplificationCSV);
            File.WriteAllLines(Path.Combine(outputPath, "SUMMARY_SIMP_Quadric_Simplification.csv"), _simpQuadricSimplificationSummaryCSV);
        }

        private static void CreatedCSVOrderedBySimplificationThenSubdivisions()
        {
            for (int s = 0; s < _subdivisonLevels.Length; s++)
            {
                for (int r = 0; r < _simplificationRates.Length; r++)
                {
                    for (int i = 0; i < _planetCount; i++)
                    {
                        var key = (_seeds[i], _subdivisonLevels[s], (int)(_simplificationRates[r] * 100));

                        var terrainGen = _testsTerrainGen[key];
                        var quadSimp = _testsQuadricSimplification[key];
                        var summaryTerrainGen = _summaryTestsTerrainGen[key];
                        var summaryQuadSimp = _summaryTestsQuadricSimplification[key];

                        _subTerrainGenCSV = AddCSVRows(_subTerrainGenCSV, terrainGen);
                        _subQuadricSimplificationCSV = AddCSVRows(_subQuadricSimplificationCSV, quadSimp);

                        _subTerrainGenSummaryCSV = [.. _subTerrainGenSummaryCSV, summaryTerrainGen];
                        _subQuadricSimplificationSummaryCSV = [.. _subQuadricSimplificationSummaryCSV, summaryQuadSimp];
                    }
                }
            }
        }

        private static void CreatedCSVOrderedBySubdivisionsThenSimplification()
        {
            for (int r = 0; r < _simplificationRates.Length; r++)
            {
                for (int s = 0; s < _subdivisonLevels.Length; s++)
                {
                    for (int i = 0; i < _planetCount; i++)
                    {
                        var key = (_seeds[i], _subdivisonLevels[s], (int)(_simplificationRates[r] * 100));

                        var terrainGen = _testsTerrainGen[key];
                        var quadSimp = _testsQuadricSimplification[key];
                        var summaryTerrainGen = _summaryTestsTerrainGen[key];
                        var summaryQuadSimp = _summaryTestsQuadricSimplification[key];

                        _simpTerrainGenCSV = AddCSVRows(_simpTerrainGenCSV, terrainGen);
                        _simpQuadricSimplificationCSV = AddCSVRows(_simpQuadricSimplificationCSV, quadSimp);

                        _simpTerrainGenSummaryCSV = [.. _simpTerrainGenSummaryCSV, summaryTerrainGen];
                        _simpQuadricSimplificationSummaryCSV = [.. _simpQuadricSimplificationSummaryCSV, summaryQuadSimp];
                    }
                }
            }
        }

        private static void RunTests()
        {
            for (int s = 0; s < _subdivisonLevels.Length; s++)
            {
                _subdivisionsA = _subdivisonLevels[s];
                // _subdivisionsB is calculated internally
                _subdivisionsC = _subdivisonLevels[s];
                _subdivisionsD = _subdivisonLevels[s];
                var upperProgress = string.Format("Subdivision: {0}/{1}", s + 1, _subdivisonLevels.Length);
                for (int r = 0; r < _simplificationRates.Length; r++)
                {
                    var innerProgress = string.Format("Simplification Rate: {0}/{1}", r + 1, _simplificationRates.Length);
                    _inputReductionRate = _simplificationRates[r];
                    for (int i = 0; i < _planetCount; i++)
                    {
                        var planetProgress = string.Format("Planet: {0}/{1}", i + 1, _planetCount);
                        Console.WriteLine(string.Format("{0}\nof {1}\nof {2}\nElapsed Time: {3}", planetProgress, innerProgress, upperProgress, Time.TimeSinceStartUpAsDouble.ToString("000000s")));
                        _seed = _seeds[i];
                        RunOnce();
                    }
                }
            }
        }

        private static void Init()
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
        }

        private static void CreateReferencePlanet()
        {
            Entity root = CreateSubdividedPlanet(World.DefaultWorld.EntityManager, 0);
            DirectSubMeshIndex[] submehses = World.DefaultWorld.EntityManager.GetComponentsInHierarchy<DirectSubMeshIndex>(root);
            // all mesehs within a planet share the same direct mesh buffer
            _reference = DirectMeshBuffer.GetMeshAtIndex(submehses[0].DirectMeshBuffer);
        }

        private static void RunOnce()
        {
            CleanUp();
            _inputReductionRate = Math.Clamp(_inputReductionRate, 0, 1);
            GetBMeshSimplificationRate();
            DoGeneration(out DirectSubMesh[] aMeshes, out DirectSubMesh[] bMeshes, out DirectSubMesh[] cMeshes, out DirectSubMesh[] dMeshes);
            Console.WriteLine(string.Format("Begin Test| Sub: {0} In Reduct: {1} Seed: {2}", _subdivisionsA, (_inputReductionRate * 100f).ToString("000"), _seed));
            GC.Collect();
            if (_enableQuadricSimplification)
            {
                DoQuadricSimplification(dMeshes);
                GC.Collect();

                _executionTimeTerrainGen.Add(CurrentTestKey, CreateCSVRowExeTime(0,aMeshes,bMeshes, _terrainGenerationTimeB));
                _executionTimeQuadricSimplification.Add(CurrentTestKey, CreateCSVRowExeTime(1, cMeshes, dMeshes, _terrainGenerationTimeD + _simplificationTimeD));
            }
            


            if (_enableDevation)
            {
                Console.WriteLine();
                Console.WriteLine("Calculating Meshes B Deviations (High Res vs Low Res Generation)");
                float maxDev = float.MinValue;
                maxDev = MathF.Max(DoDevation(0, aMeshes, bMeshes),maxDev);

                GC.Collect();

                Console.WriteLine();
                Console.WriteLine("Calculating Meshes D Deviations (High Res vs Quadric Simplified)");
                maxDev = MathF.Max(DoDevation(1, cMeshes, dMeshes), maxDev);
                SetUV_Y(maxDev,aMeshes,bMeshes,cMeshes,dMeshes);
                GC.Collect();
            }

            Console.WriteLine(string.Format("\nInput reduct-rate: {0}% | Actual: {1}% | Subdivisions (Mesh B) {2}", (_inputReductionRate * 100f).ToString("00.00"), (_actualReductionRate * 100f).ToString("00.00"), _subdivisionsB));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Meshes A-B (High Res vs Low Res Generation");
            GetStats(aMeshes, bMeshes);
            Console.WriteLine();
            Console.WriteLine("Meshes C-D (High Res vs Quadric Simplified)");
            GetStats(cMeshes, dMeshes);
            Console.WriteLine();
            CalculateVertsAndTris(aMeshes, out uint Hverts, out uint Htris);
            Console.WriteLine(string.Format("Seed: {0}, Src Vert Count: {1} Src Tri Count: {2}", _seed, Hverts, Htris / 3));

            GC.Collect();
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

            Entity a = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsA);
            Entity b = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsB);
            _terrainGenerationTimeB = _stopwatch.Elapsed.TotalMilliseconds;
            Entity c = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsC);
            Entity d = GenerateSubdividedPlanet(shapeGenerator, World.DefaultWorld.EntityManager, _subdivisionsD);
            _terrainGenerationTimeD = _stopwatch.Elapsed.TotalMilliseconds;

            _rootEntities = [a, b, c, d];

            World.DefaultWorld.EntityManager.SetComponent(a, new Translation() { Value = new(5f, 0, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(b, new Translation() { Value = new(5, 0, -5f) });
            World.DefaultWorld.EntityManager.SetComponent(c, new Translation() { Value = new(-5f, 2, 5f) });
            World.DefaultWorld.EntityManager.SetComponent(d, new Translation() { Value = new(-5f, 2, -5f) });

            _stopwatch.Restart();
            aMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager, a);
            bMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager, b);
            cMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager, c);
            dMeshes = GetMeshesInChildren(World.DefaultWorld.EntityManager, d);

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

            //float[] estimatedErrors = new float[aMeshes.Length];

            _stopwatch.Restart();
            Parallel.For(0, aMeshes.Length, parallelOptions, (int i) =>
            {
                //estimatedErrors[i] = Simplify(aMeshes[i]);
                Simplify(aMeshes[i]);
            });

            aMeshes[0].DirectMeshBuffer.FlushAll();
            _stopwatch.Stop();
            _simplificationTimeD = _stopwatch.Elapsed.TotalMilliseconds;
            //if (_logSimplificationRMS)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("Logging simplification Estiamte RMS");
            //    for (int i = 0; i < aMeshes.Length; i++)
            //    {
            //        Console.WriteLine(string.Format("RMS error {0}", estimatedErrors[i]));
            //    }
            //}
            Console.WriteLine();
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
                faceCount = (int)MathF.Floor(mesh.IndexCount / 3 * _actualReductionRate)
            };
            var meshDecimation = new UnityMeshDecimation();
            meshDecimation.Execute(mesh, parameter, conditions);
            meshDecimation.ToMesh(mesh);
            //return meshDecimation.EstimatedError;
        }

        private static float DoDevation(int method, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes)
        {
            if (_interAllTiles)
            {
                _tileIterCount = aMeshes.Length;
            }
            _stopwatch.Restart();
            
            string[] stats = new string[_tileIterCount];
            Vector3 elevationMeans = new(float.MaxValue, float.MinValue, 0);
            Vector3 meanDevStats = new(float.MaxValue, float.MinValue, 0);

            aMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            bMeshes[0].DirectMeshBuffer.ForceCrunchFaceData();
            float maxDev = float.MinValue;
            for (int i = 0; i < _tileIterCount; i++)
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


                var devation = new Deviation();

                devation.Initialization(aMeshes[i], bMeshes[i]);
                devation.Compute(_normalDevation, _parallelDevation);
                
                //devation.Deviation2Material();

                uint bverts = bMeshes[i].VertexCount;
                uint btris = bMeshes[i].IndexCount;
                btris /= 3;
                Vector2 actualReductionRates = CalculateResultantSimplificationRates(aMeshes[i], bMeshes[i]);
                stats[i] = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}",
                    _seed,
                    i,
                    method,
                    _subdivisionsA,
                    (int)(_inputReductionRate*100),
                    bverts,
                    btris,
                    actualReductionRates.X,
                    actualReductionRates.Y,
                    minElevation,
                    maxElevation,
                    meanElevation,
                    devation.GetCSVStatisticRow());

                var devData = devation.GetDataForMean();

                meanDevStats.X = MathF.Min(devData.X, meanDevStats.X);
                meanDevStats.Y = MathF.Max(devData.Y, meanDevStats.Y);
                meanDevStats.Z += devData.Z;

                maxDev = MathF.Max(devation.DevBound, maxDev);
            }

            elevationMeans.Z /= _tileIterCount;
            meanDevStats.Z /= _tileIterCount;

            _stopwatch.Stop();

            Console.WriteLine(string.Format("Devation Calc: {0}ms", _stopwatch.Elapsed.TotalMilliseconds));

            if(method == 0)
            {
                _testsTerrainGen.Add(CurrentTestKey, stats);
                //_terrainGenCSV = AddCSVRows(_terrainGenCSV, stats);
                //_terrainGenSummaryCSV = [.. _terrainGenSummaryCSV, CreateSummaryCSVRow(method, aMeshes, bMeshes, elevationMeans, meanDevStats)];
                _summaryTestsTerrainGen.Add(CurrentTestKey, CreateSummaryCSVRowDev(method, aMeshes, bMeshes, elevationMeans, meanDevStats));
            }
            else
            {
                _testsQuadricSimplification.Add(CurrentTestKey, stats);
                //_quadricSimplificationCSV = AddCSVRows(_quadricSimplificationCSV, stats);
                //_quadricSimplificationSummaryCSV = [.. _quadricSimplificationSummaryCSV, CreateSummaryCSVRow(method, aMeshes, bMeshes, elevationMeans, meanDevStats)];
                _summaryTestsQuadricSimplification.Add(CurrentTestKey, CreateSummaryCSVRowDev(method, aMeshes, bMeshes, elevationMeans, meanDevStats));
            }

            return maxDev;
        }

        private static void SetUV_Y(float maxDev,DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, DirectSubMesh[] cMeshes, DirectSubMesh[] dMeshes)
        {
            SetUV_Ys(maxDev, aMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, bMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, cMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, dMeshes[0].DirectMeshBuffer);
        }

        private static void SetUV_Ys(float maxDev, DirectMeshBuffer directMesh)
        {
            var uvs = directMesh.GetFullVertexData<Vector2>(VertexAttribute.TexCoord0);
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i].Y = maxDev;
            }
            directMesh.FlushAll();
            DirectMeshBuffer.RecalcualteAllNormals(directMesh);
        }

        private static string CreateCSVRowExeTime(int method, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, double executionTime)
        {
            CalculateVertsAndTris(bMeshes, out uint bverts, out uint btris);
            var actualReductionRates = CalculateSimplificationRates(aMeshes, bMeshes);

            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                _seed,
                0,
                method,
                _subdivisionsA,
                (int)(_inputReductionRate * 100),
                bverts,
                btris,
                actualReductionRates.X,
                actualReductionRates.Y,
                executionTime
            );
        }

        private static string CreateSummaryCSVRowDev(int method, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, Vector3 elevationData, Vector3 deviationData)
        {
            CalculateVertsAndTris(bMeshes, out uint bverts, out uint btris);
            var actualReductionRates = CalculateSimplificationRates(aMeshes, bMeshes);

            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}",
                _seed,
                0,
                method,
                _subdivisionsA,
                (int)(_inputReductionRate * 100),
                bverts,
                btris,
                actualReductionRates.X,
                actualReductionRates.Y,
                elevationData.X,
                elevationData.Y,
                elevationData.Z,
                deviationData.X,
                deviationData.Y,
                deviationData.Z
            );
        }

        private static string[] AddCSVRows(string[] csv, string[] row)
        {
            return [.. csv, .. row];
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
        }

        private static void GetBMeshSimplificationRate()
        {
            int subdivisonsForB = _subdivisionsA;
            uint currentIndexCount = GetSubdivisonCounts(0, _subdivisionsA).Y;
            int dstFromTarget = int.MaxValue;
            uint targetIndexCount = (uint)MathF.Floor(currentIndexCount * _inputReductionRate);
            for (int i = _subdivisionsA; i > 0; i--)
            {
                int newDstFromTarget = Math.Abs((int)targetIndexCount - (int)GetSubdivisonCounts(0, i).Y);

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
            uint calculatedIndexCount = GetSubdivisonCounts(0, subdivisonsForB).Y;

            _actualReductionRate = (float)calculatedIndexCount / (float)currentIndexCount;

            _subdivisionsB = subdivisonsForB;
        }

        private static Vector2 CalculateSimplificationRates(DirectSubMesh[] a, DirectSubMesh[] b)
        {
            CalculateVertsAndTris(a, out uint aV, out uint aT);
            CalculateVertsAndTris(b, out uint bV, out uint bT);
        
            return new((float)bV / (float)aV, (float)bT / (float)aT);
        }

        private static Vector2 CalculateResultantSimplificationRates(DirectSubMesh a, DirectSubMesh b)
        {
            uint aV = a.VertexCount;
            uint aT = a.IndexCount;
            uint bV = b.VertexCount;
            uint bT = b.IndexCount;

            return new ((float)bV / (float)aV, (float)bT / (float)aT);
        }

        private static Vector2UInt GetSubdivisonCounts(int tile, int subdivisions)
        {
            var vertices = MeshExtensions.GetVertsPerFace(subdivisions);
            var indices = MeshExtensions.GetIndicesPerFace(subdivisions);
            var currentIndices = _reference.SubMeshInfos[tile].IndexCount;

            vertices *= currentIndices / 3;
            indices *= currentIndices / 3;

            return new (vertices, indices);
        }

        private static void GetStats(DirectSubMesh[] highRes, DirectSubMesh[] lowRes)
        {
            CalculateVertsAndTris(lowRes, out uint Lverts, out uint Ltris);
            CalculateVertsAndTris(highRes, out uint Hverts, out uint Htris);

            Console.WriteLine(string.Format("Low res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Lverts, Ltris, Ltris / 3));
            Console.WriteLine(string.Format("High res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Hverts, Htris, Htris / 3));
            Console.WriteLine(string.Format("Reduction rates: Verts: {0}%, Indices: {1}%", (((float)Lverts / (float)Hverts) * 100f).ToString("00.00"), (((float)Ltris / (float)Htris) * 100f).ToString("00.00")));
        }

        private static void CalculateVertsAndTris(DirectSubMesh[] lowRes, out uint verts, out uint indices)
        {
            verts = 0;
            indices = 0;
            for (int i = 0; i < lowRes[0].DirectMeshBuffer.SubMeshInfos.Length; i++)
            {
                verts += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].VertexCount;
                indices += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].IndexCount;
            }
        }

        public static DirectSubMesh[] GetMeshesInChildren(EntityManager entityManager, Entity parent)
        {
            Entity[] entityMeshes = entityManager.GetComponent<Children>(parent).Value;
            DirectSubMesh[] meshes = new DirectSubMesh[entityMeshes.Length];
            for (int j = 0; j < entityMeshes.Length; j++)
            {
                meshes[j] = DirectSubMesh.GetSubMeshAtIndex(entityManager.GetComponent<DirectSubMeshIndex>(entityMeshes[j]));
            }
            return meshes;
        }

        private static Entity GenerateSubdividedPlanet(ShapeGenerator shapeGenerator,EntityManager entityManager, int subdivisons)
        {
            Entity planet = CreateSubdividedPlanet(entityManager, subdivisons);

            entityManager.RemoveComponentFromHierarchy<DoNotRender>(planet);
            entityManager.RemoveComponentFromHierarchy<Prefab>(planet);
            _stopwatch.Restart();
            ArtifactAuthoring.GeneratePlanet(planet, shapeGenerator);
            _stopwatch.Stop();
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

        public static void TestDevation(EntityManager entityManager)
        {
            var sphere = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("Sphere.obj"), null);
            var cube = MeshLoader.LoadModelFromFile(MeshLoader.GetMeshInDefaultPath("cube-UV.obj"), null);

            var sphereEntity = entityManager.CreateEntity();
            var cubeEntity = entityManager.CreateEntity();


            entityManager.AddComponent(sphereEntity, sphere[0].GetSubMeshIndex());
            entityManager.AddComponent(sphereEntity, new Translation() { Value = new(0, 0f, 0) });
            entityManager.AddComponent(sphereEntity, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent(sphereEntity, new MaterialIndex { Value = Material.GetIndexOfMaterial(_deviationHeatMap) });

            entityManager.AddComponent(cubeEntity, cube[0].GetSubMeshIndex());
            entityManager.AddComponent(cubeEntity, new Translation() { Value = new(0, 0f, 0) });
            entityManager.AddComponent(cubeEntity, new Scale() { Value = new(3f, 3f, 3f) });
            entityManager.AddComponent(cubeEntity, new MaterialIndex { Value = Material.GetIndexOfMaterial(_deviationHeatMap) });

            DoDevation(0,sphere, cube);

        }
    }
}
