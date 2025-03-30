using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using VECS;

namespace COMP302
{
    public class CSVDataHandler
    {
        // algorithim = 0 = terrain generator
        // algorithim = 1 = Quadric Simplification
        private static string _resultOutputPath = "Results/Test-13";
        public static string ResultOutputPath => _resultOutputPath;
        private static readonly string[] _csvHeaderDev = ["Seed, Tile_ID, Algorithm, Src_SubDiv, Input_Reduction, Vert_Count, Tri_Count, Vert_Reduction, Tri_Reduction, Min_Elev, Max_Elev, Mean_Elev, Min_Dev, Max_Dev, Mean_Dev"];
        private static readonly string[] _csvHeaderExeTime = ["Seed, Tile_ID, Algorithm, Src_SubDiv, Input_Reduction, Vert_Count, Tri_Count, Vert_Reduction, Tri_Reduction, Execution_Time"];

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

        private static string[] _exeTimeSubBySimpTerrainGenCSV = _csvHeaderExeTime;
        private static string[] _exeTimeSubBySimpQuadricSimplificationCSV = _csvHeaderExeTime;


        public static readonly Dictionary<(int, int, int), string[]> _testsTerrainGen = [];
        public static readonly Dictionary<(int, int, int), string[]> _testsQuadricSimplification = [];

        public static readonly Dictionary<(int, int, int), string> _summaryTestsTerrainGen = [];
        public static readonly Dictionary<(int, int, int), string> _summaryTestsQuadricSimplification = [];

        public static readonly Dictionary<(int, int, int), string> _executionTimeTerrainGen = [];
        public static readonly Dictionary<(int, int, int), string> _executionTimeQuadricSimplification = [];

        public static void SetOutputDirectory()
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
                Program.InvalidInput(newDirectory, SetOutputDirectory);
            }
        }

        public static void ExportData()
        {
            string outputPath = Path.Combine(Application.ExecutingDirectory, _resultOutputPath);
            Directory.CreateDirectory(outputPath);
            if (ExpirmentConfig.LogDeviations)
            {

                CreatedCSVOrderedBySimplificationThenSubdivisions();
                CreatedCSVOrderedBySubdivisionsThenSimplification();

                ExportDeviationCSVs(outputPath);
            }

            if (ExpirmentConfig.LogExecutionTime)
            {
                Created_EXE_TIME_CSVOrderedBySimplificationThenSubdivisions();
                Created_EXE_TIME_CSVOrderedBySubdivisionsThenSimplification();

                ExportExecutionTimeCSV(outputPath);
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
            for (int s = 0; s < ExpirmentConfig.SubdivisonLevels.Length; s++)
            {
                for (int r = 0; r < ExpirmentConfig.SimplificationRates.Length; r++)
                {
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        var key = (ExpirmentConfig.Seeds[i], ExpirmentConfig.SubdivisonLevels[s], (int)(ExpirmentConfig.SimplificationRates[r] * 100));

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
            for (int r = 0; r < ExpirmentConfig.SimplificationRates.Length; r++)
            {
                for (int s = 0; s < ExpirmentConfig.SubdivisonLevels.Length; s++)
                {
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        var key = (ExpirmentConfig.Seeds[i], ExpirmentConfig.SubdivisonLevels[s], (int)(ExpirmentConfig.SimplificationRates[r] * 100));

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
            for (int s = 0; s < ExpirmentConfig.SubdivisonLevels.Length; s++)
            {
                for (int r = 0; r < ExpirmentConfig.SimplificationRates.Length; r++)
                {
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        var key = (ExpirmentConfig.Seeds[i], ExpirmentConfig.SubdivisonLevels[s], (int)(ExpirmentConfig.SimplificationRates[r] * 100));

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
            for (int r = 0; r < ExpirmentConfig.SimplificationRates.Length; r++)
            {
                for (int s = 0; s < ExpirmentConfig.SubdivisonLevels.Length; s++)
                {
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        var key = (ExpirmentConfig.Seeds[i], ExpirmentConfig.SubdivisonLevels[s], (int)(ExpirmentConfig.SimplificationRates[r] * 100));

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

        public static void AddDeviationData((int , int , int )key, int method, string[] stats, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, Vector3 elevationData, Vector3 deviationData)
        {
            if (method == 0)
            {
                _testsTerrainGen.Add(key, stats);
                //_terrainGenCSV = AddCSVRows(_terrainGenCSV, stats);
                //_terrainGenSummaryCSV = [.. _terrainGenSummaryCSV, CreateSummaryCSVRow(method, aMeshes, bMeshes, elevationMeans, meanDevStats)];
                _summaryTestsTerrainGen.Add(key, CreateSummaryCSVRowDev(method, key, aMeshes, bMeshes, elevationData, deviationData));
            }
            else
            {
                _testsQuadricSimplification.Add(key, stats);
                //_quadricSimplificationCSV = AddCSVRows(_quadricSimplificationCSV, stats);
                //_quadricSimplificationSummaryCSV = [.. _quadricSimplificationSummaryCSV, CreateSummaryCSVRow(method, aMeshes, bMeshes, elevationMeans, meanDevStats)];
                _summaryTestsQuadricSimplification.Add(key, CreateSummaryCSVRowDev(method,key, aMeshes, bMeshes, elevationData, deviationData));
            }
        }

        public static void AddExeuctionData((int, int, int) key, double genTimeB, double genTimeD,double simpTimeD, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, DirectSubMesh[] cMeshes, DirectSubMesh[] dMeshes)
        {
            _executionTimeTerrainGen.Add(key, CreateCSVRowExeTime(0, key, aMeshes, bMeshes, genTimeB));
            _executionTimeQuadricSimplification.Add(key, CreateCSVRowExeTime(1, key, cMeshes, dMeshes, genTimeD + simpTimeD));
        }

        private static string CreateSummaryCSVRowDev(int method, (int, int, int) key, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, Vector3 elevationData, Vector3 deviationData)
        {
            ExpirmentUtilities.CalculateVertsAndTris(bMeshes, out uint bverts, out uint btris);
            var actualReductionRates = ExpirmentUtilities.CalculateSimplificationRates(aMeshes, bMeshes);

            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}",
                key.Item1,
                0,
                method,
                key.Item2,
                key.Item3,
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


        private static string CreateCSVRowExeTime(int method, (int, int, int) key, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, double executionTime)
        {
            ExpirmentUtilities.CalculateVertsAndTris(bMeshes, out uint bverts, out uint btris);
            var actualReductionRates = ExpirmentUtilities.CalculateSimplificationRates(aMeshes, bMeshes);

            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                key.Item1,
                0,
                method,
                key.Item2,
                key.Item3,
                bverts,
                btris,
                actualReductionRates.X,
                actualReductionRates.Y,
                executionTime
            );
        }


        private static string[] AddCSVRows(string[] csv, string[] row)
        {
            return [.. csv, .. row];
        }
    }
}
