using COMP302;

namespace COMP302Test
{
    public class TestInput : InputInterface
    {
        public string[] _inputs = ["0"];
        private int index = 0;

        public void SetInputs(string[] inputs)
        {
            _inputs = inputs;
            index = 0;
        }

        public void Reset()
        {
            _inputs = ["0"];
            index = 0;
        }

        public override string GetNextInput()
        {
            var input = _inputs[index];
            index = (index + 1) % _inputs.Length;
            Console.WriteLine(input);
            return input;
        }
    }
}
