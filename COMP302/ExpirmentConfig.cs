using System;

namespace COMP302
{
    public static class ExpirmentConfig
    {
        private static int _planetCount = 10;
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
        private static int[] _subdivisonLevels = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];
        private static float[] _reductionRates = [0.95f, 0.90f, 0.85f, 0.80f, 0.75f, 0.70f, 0.65f, 0.60f, 0.55f, 0.50f, 0.45f, 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, 0.05f];

        public static readonly bool RunAllSubdivisions = true;

        // simplification settings
        public static readonly bool EnableQuadricSimplification = true;
        public static readonly bool LogSimplificationRMS = false;
        public static readonly bool RunAllReductionRates = true;

        // geometric devation settings
        public static readonly bool EnableDevation = true;
        public static readonly bool NormalDevation = false;
        public static readonly bool ParallelDevation = true;
        public static readonly bool LogDeviations = false;
        public static readonly bool LogExecutionTime = false;
        private static bool _testDeviation = false;
        private static bool _testSimplification = false;

        public static int PlanetCount => _planetCount;
        public static int[] Seeds => _seeds;
        public static int[] SubdivisonLevels => _subdivisonLevels;
        public static float[] ReductionRates => _reductionRates;
        public static bool TestSimplification { get => _testSimplification; set => _testSimplification = value; }

        public static bool TestDeviation { get => _testDeviation; set => _testDeviation = value; }

        public static void Reset()
        {
            _testDeviation = false;
            _testSimplification = false;
            _planetCount = 10;
            _seeds = [
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
            _subdivisonLevels = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];
            _reductionRates = [0.95f, 0.90f, 0.85f, 0.80f, 0.75f, 0.70f, 0.65f, 0.60f, 0.55f, 0.50f, 0.45f, 0.40f, 0.35f, 0.30f, 0.25f, 0.20f, 0.15f, 0.10f, 0.05f];
        }


        public static void GenerateSeedsInput()
        {
            SetPlanetCount(SetAndGeneratePlanetSeeds);
        }

        private static void SetAndGeneratePlanetSeeds(int plants)
        {
            _planetCount = plants;
            Console.WriteLine(string.Format("Set planet count to: {0}", _planetCount));
            GenerateSeeds();
            Console.WriteLine("Generated Seeds Set\n\n");
        }

        private static void GenerateSeeds()
        {
            _seeds = new int[_planetCount];
            for (int i = 0; i < _planetCount; i++)
            {
                _seeds[i] = Random.Shared.Next(int.MinValue, int.MaxValue);
            }
        }

        public static void SetSeeds()
        {
            SetPlanetCount(EnterSeeds);
        }

        private static void SetPlanetCount(Action<int> successCallback)
        {
            Console.WriteLine(string.Format("\n\nCurrent planet count: {0}", _planetCount));
            Console.WriteLine(string.Format("Enter planet count\n"));
            string plantCountText = Program.InputInterface.GetNextInput();
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
                Program.InvalidInput(plantCountText, GenerateSeedsInput);
            }
        }

        private static void EnterSeeds(int planetCount)
        {
            _planetCount = planetCount;
            Console.WriteLine("Set planet count to: {0}", _planetCount);
            _seeds = new int[_planetCount];
            for (int i = 0; i < _planetCount; i++)
            {
                Console.WriteLine("Enter seed for planet: {0}", i + 1);
                string seedInput = Program.InputInterface.GetNextInput();
                if (int.TryParse(seedInput, out int seed))
                {
                    Seeds[i] = seed;
                    Console.WriteLine("Planet: {0} Seed Set to: {1}\n\n", i + 1, seed);
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}", seedInput);
                    i--;
                }
            }
            Console.WriteLine("All Seeds Set\n\n");
        }

        public static void SetSubdivisionLevels()
        {
            Console.WriteLine(string.Format("\n\nCurrent number of subdivision steps: {0}", SubdivisonLevels.Length));
            Console.WriteLine(string.Format("Enter subdivision count\n"));
            string subdivisionCountText = Program.InputInterface.GetNextInput();
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
                Program.InvalidInput(subdivisionCountText, SetSubdivisionLevels);
                return;
            }

            for (int i = 0; i < SubdivisonLevels.Length; i++)
            {
                Console.WriteLine("Enter subidivions for step: {0}", i + 1);
                string subdivisionInput = Program.InputInterface.GetNextInput();
                if (int.TryParse(subdivisionInput, out int divisions) && divisions > 0)
                {
                    SubdivisonLevels[i] = divisions;
                    Console.WriteLine("Subdivision Step: {0} Set to: {1}\n\n", i + 1, divisions);
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}, must be greater than 0", subdivisionInput);
                    i--;
                }
            }
            Console.WriteLine("All subdivisions Set\n\n");
        }

        public static void SetInputReductionRates()
        {
            Console.WriteLine(string.Format("\n\nCurrent number of Reduction levels: {0}", ReductionRates.Length));
            Console.WriteLine(string.Format("Enter number of reduction levels\n"));
            string reductionCountText = Program.InputInterface.GetNextInput();
            if (int.TryParse(reductionCountText, out int result))
            {
                if (result < 1)
                {
                    Console.WriteLine(string.Format("\n\nValue out of range Invalid \"{0}\"\nValue must be greater or equal to 1.", result));
                    SetInputReductionRates();
                }
                _reductionRates = new float[result];
            }
            else
            {
                Program.InvalidInput(reductionCountText, SetInputReductionRates);
                return;
            }


            for (int i = 0; i < ReductionRates.Length; i++)
            {
                Console.WriteLine("Enter reduction rate for step: {0}, this should be a fractional value (0-1) where 0.5 => 50%", i + 1);
                string simplificationRate = Program.InputInterface.GetNextInput();
                if (float.TryParse(simplificationRate, out float reductionRate) && reductionRate > 0)
                {
                    ReductionRates[i] = reductionRate;
                    Console.WriteLine("Reduction rate level: {0} Set to: {1}%\n\n", i + 1, (ReductionRates[i] * 100).ToString("00.00"));
                }
                else
                {
                    Console.WriteLine("Invalid input: {0}, must be >= 0 and <= 1", simplificationRate);
                    i--;
                }
            }
            Console.WriteLine("All Reduction rates Set\n\n");
        }

    }
}
