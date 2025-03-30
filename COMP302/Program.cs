using VECS;
using Planets;
using System.Reflection;
using System;

namespace COMP302
{
    public class Program
    {
        private static ArtifactAuthoring artifactAuthoring;
        
        static int Main()
        {
            try
            {
                Assembly.Load("Planets");
                Application app = new();
                app.PreOnCreate += CreateArtifact;
                app.OnDestroy += DestroyArtifact;
                app.Run();
                app.PreOnCreate -= CreateArtifact;
                app.OnDestroy -= DestroyArtifact;
                app.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0},\n{1}", ex.Message, ex.StackTrace));
                return 1;
            }
            return 0;
        }

        static void CreateArtifact()
        {
            artifactAuthoring = new();
            MainMenu();
        }

        static void DestroyArtifact()
        {
            ArtifactAuthoring.Destroy();
        }

        public static void MainMenu()
        {
            Console.WriteLine("Select option (0-13)");
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
            Console.WriteLine("12. Test Geometric Deviation (Sphere/cube)");
            Console.WriteLine("13. Test QSlim (Stanford bunny, 69451 Tris to 999 Tris)");
            string input = Console.ReadLine();
            if (!int.TryParse(input, out int results))
            {
                InvalidInput(input, MainMenu);
            }
            switch (results)
            {
                case 0:
                    Application.Exit();
                    return;
                case 1:
                    for (int i = 0; i < ExpirmentConfig.PlanetCount; i++)
                    {
                        Console.WriteLine(string.Format("Planet Seed {0}: {1}", i + 1, ExpirmentConfig.Seeds[i]));
                    }
                    break;
                case 2:
                    ExpirmentConfig.GenerateSeedsInput();
                    break;
                case 3:
                    ExpirmentConfig.SetSeeds();
                    break;
                case 4:
                    for (int i = 0; i < ExpirmentConfig.SubdivisonLevels.Length; i++)
                    {
                        Console.WriteLine(string.Format("Subdivision level {0}: {1} Divisions", i + 1, ExpirmentConfig.SubdivisonLevels[i]));
                    }
                    break;
                case 5:
                    ExpirmentConfig.SetSubdivisionLevels();
                    break;
                case 6:
                    for (int i = 0; i < ExpirmentConfig.SimplificationRates.Length; i++)
                    {
                        Console.WriteLine(string.Format("Reduction level {0}: {1}% of original geometry", i + 1, (ExpirmentConfig.SimplificationRates[i] * 100).ToString("00.00")));
                    }
                    break;
                case 7:
                    ExpirmentConfig.SetInputReductionRates();
                    break;
                case 8:
                    Expirment.VisualiseResult();
                    return;
                case 9:
                    Expirment.Run();
                    return;
                case 10:
                    Console.WriteLine(CSVDataHandler.ResultOutputPath);
                    break;
                case 11:
                    CSVDataHandler.SetOutputDirectory();
                    break;
                case 12:
                    ExpirmentConfig.TestDeviation = true;
                    Expirment.Run();
                    return;
                case 13:
                    ExpirmentConfig.TestSimplification = true;
                    Expirment.Run();
                    return;
                default:
                    InvalidInput(results.ToString(), null);
                    break;
            }
            Console.WriteLine("\n\n");
            MainMenu();
        }

        public static void InvalidInput(string input, Action callback)
        {
            Console.WriteLine(string.Format("Invalid input: \"{0}\"", input));
            callback?.Invoke();
        }
    }
}
