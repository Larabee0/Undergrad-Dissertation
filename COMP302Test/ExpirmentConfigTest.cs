using COMP302;
using VECS;

namespace COMP302Test
{
    [DoNotParallelize]
    [TestClass]
    public sealed class ExpirmentConfigTest
    {
        private TestInput testInput;

        private Thread executionThread = null;
        private Mutex _restartMutex = new();

        [TestMethod, DoNotParallelize]
        public void TestGenerateSeeds()
        {
            SetInputToTestInput();
            for (int i = 1; i < 11; i++)
            {
                var seeds = ExpirmentConfig.Seeds;
                testInput.SetInputs([i.ToString(), 0.ToString()]);
                ExpirmentConfig.GenerateSeedsInput();

                Assert.AreNotSame(seeds, ExpirmentConfig.Seeds);
            }
        }

        [TestMethod, DoNotParallelize]
        public void TestClose()
        {
            SetInputToTestInput();
            testInput.SetInputs(["0"]);
            StartApplication();
            ValidateApplicationStopped();
        }

        [TestMethod, DoNotParallelize]
        public void TestCloseAndOpenAndClose()
        {
            SetInputToTestInput();
            testInput.SetInputs(["12"]);
            StartApplication();
            Assert.IsNotNull(Program.ArtifactAuthoring);
            ValidateApplicationStopped();

            TestClose();
        }

        private void ValidateApplicationStopped()
        {
            Thread.Sleep(10);
            Application.Exit();
            _restartMutex.WaitOne();
            _restartMutex.ReleaseMutex();
            executionThread = null;
            Assert.IsNull(Program.ArtifactAuthoring);
        }

        private void StartApplication()
        {
            executionThread = new Thread(() =>
            {
                _restartMutex.WaitOne();
                Program.InternalStart();
                _restartMutex.ReleaseMutex();
            });
            executionThread.Start();
            Thread.Sleep(1000);
        }

        private void SetInputToTestInput()
        {
            Program.InputInterface = testInput = new TestInput();
        }
    }
}
