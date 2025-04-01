using COMP302;
using VECS;

namespace COMP302Test
{
    [TestClass]
    public sealed class CLITest
    {
        private TestInput testInput;

        private Thread _executionThread = null;
        private readonly Mutex _restartMutex = new();

        [TestMethod]
        public void TestGenerateSeeds()
        {
            SetInputToTestInput();
            for (int i = 1; i < 11; i++)
            {
                var seeds = ExpirmentConfig.Seeds;
                testInput.SetInputs(["2",i.ToString(), 0.ToString()]);
                ExpirmentConfig.GenerateSeedsInput();

                Assert.AreNotSame(seeds, ExpirmentConfig.Seeds);
            }
        }

        [TestMethod]
        public void TestSetSeeds()
        {
            SetInputToTestInput();

            for (int i = 1; i < 11; i++)
            {
                var seeds = ExpirmentConfig.Seeds;

                string[] instructions = ["3", i.ToString()];
                for (int j = 0; j < i; j++)
                {
                    instructions = [..instructions, Random.Shared.Next(int.MinValue, int.MaxValue).ToString()];
                }
                instructions = [.. instructions, "0"];
                testInput.SetInputs(instructions);
                ExpirmentConfig.SetSeeds();

                Assert.AreNotSame(seeds, ExpirmentConfig.Seeds);
            }
        }

        [TestMethod]
        public void TestSetSubdivisions()
        {
            SetInputToTestInput();
            for (int i = 1; i < 11; i++)
            {
                var divisions = ExpirmentConfig.SubdivisonLevels;

                string[] instructions = ["5", i.ToString()];
                for (int j = 0; j < i; j++)
                {
                    instructions = [.. instructions, Random.Shared.Next(0, 200).ToString()];
                }
                instructions = [.. instructions, "0"];
                testInput.SetInputs(instructions);
                ExpirmentConfig.SetSubdivisionLevels();

                Assert.AreNotSame(divisions, ExpirmentConfig.SubdivisonLevels);
            }
        }

        [TestMethod]
        public void TestSetReductionRates()
        {
            SetInputToTestInput();
            for (int i = 1; i < 11; i++)
            {
                var reductionRates = ExpirmentConfig.ReductionRates;

                string[] instructions = ["7", i.ToString()];
                for (int j = 0; j < i; j++)
                {
                    instructions = [.. instructions, (Random.Shared.Next(0, 10000)/(float)1000).ToString()];
                }
                instructions = [.. instructions, "0"];
                testInput.SetInputs(instructions);
                ExpirmentConfig.SetInputReductionRates();

                Assert.AreNotSame(reductionRates, ExpirmentConfig.ReductionRates);
            }
        }

        [TestMethod]
        public void TestSetOutDir()
        {
            SetInputToTestInput();

            for (int i = 1; i < 11; i++)
            {
                CSVDataHandler.Reset();
                var path = CSVDataHandler.ResultOutputPath;
                string[] instructions = ["11", string.Format("Results/{0}", i), "0"];
                testInput.SetInputs(instructions);
                Program.MainMenu();

                Assert.AreNotSame(path, CSVDataHandler.ResultOutputPath);
                Assert.AreEqual(CSVDataHandler.ResultOutputPath, string.Format("Results/{0}", i));
            }
        }

        [TestMethod]
        public void TestVisualise()
        {

            for (int i = 0; i < 4; i++)
            {
                SetInputToTestInput();
                string[] instructions = ["8", // instruction 8 visualise
                    Random.Shared.Next(1, ExpirmentConfig.PlanetCount).ToString(), // set planet seed
                    Random.Shared.Next(1, ExpirmentConfig.SubdivisonLevels.Length).ToString(), // set subdivision level
                    Random.Shared.Next(1, ExpirmentConfig.ReductionRates.Length).ToString()]; // set reduction rate
                testInput.SetInputs(instructions);
                StartApplication();
                Assert.IsNotNull(Program.ArtifactAuthoring);
                Console.WriteLine("Waiting for result");
                StopAndValidateStopped();
            }
        }

        [TestMethod]
        public void TestRunExpirment()
        {
            CSVDataHandler.Reset();
            string outputDir = Path.Combine(Application.ExecutingDirectory, CSVDataHandler.ResultOutputPath);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir);
            }
            SetInputToTestInput();
            string[] instructions = ["2", // gen seed instruction
                    "2", // set planet count
                    "5", // set subdivision levels instruction
                    "2", // set subdivision Count
                    "10", // first level
                    "20", // second level
                    "7", // set reduction rates instruction
                    "2", // reduction rate count
                    "0.5", // level 1
                    "0.75", // level 2
                    "9", // run tests as configured
                    "0" // exit
            ];
            testInput.SetInputs(instructions);
            StartApplication();
            Assert.IsNotNull(Program.ArtifactAuthoring);
            Console.WriteLine("Waiting for result");
            StopAndValidateStopped();
            Assert.IsTrue(Directory.Exists(outputDir));
        }

        [TestMethod]
        public void TestCloseAndOpenAndClose()
        {
            SetInputToTestInput();
            testInput.SetInputs(["12"]);
            StartApplication();
            Assert.IsNotNull(Program.ArtifactAuthoring);
            StopAndValidateStopped();

            TestClose();
        }

        [TestMethod]
        public void TestGeometricDeviation()
        {
            SetInputToTestInput();
            testInput.SetInputs(["12"]);
            StartApplication();
            Assert.IsNotNull(Program.ArtifactAuthoring);
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestQSlim()
        {
            SetInputToTestInput();
            testInput.SetInputs(["13"]);
            StartApplication();
            Assert.IsNotNull(Program.ArtifactAuthoring);
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestViewOutDir()
        {
            SetInputToTestInput();
            testInput.SetInputs(["10","0"]);
            StartApplication();
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestViewSeeds()
        {
            SetInputToTestInput();
            testInput.SetInputs(["1", "0"]);
            StartApplication();
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestViewSubdivisionLevels()
        {
            SetInputToTestInput();
            testInput.SetInputs(["4", "0"]);
            StartApplication();
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestViewReductionLevels()
        {
            SetInputToTestInput();
            testInput.SetInputs(["6", "0"]);
            StartApplication();
            StopAndValidateStopped();
        }

        [TestMethod]
        public void TestClose()
        {
            SetInputToTestInput();
            testInput.SetInputs(["0"]);
            StartApplication();
            StopAndValidateStopped();
        }

        private void StopAndValidateStopped()
        {
            Thread.Sleep(1000);
            Application.Exit();
            _restartMutex.WaitOne();
            _restartMutex.ReleaseMutex();
            _executionThread = null;
            Assert.IsNull(Program.ArtifactAuthoring);
        }

        private void StartApplication()
        {
            _executionThread = new Thread(() =>
            {
                _restartMutex.WaitOne();
                Program.InternalStart();
                _restartMutex.ReleaseMutex();
            });
            _executionThread.Start();
            Thread.Sleep(1000);
        }

        private void SetInputToTestInput()
        {
            Program.InputInterface = testInput = new TestInput();
        }
    }
}
