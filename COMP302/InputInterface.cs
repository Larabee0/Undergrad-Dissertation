using System;

namespace COMP302
{
    public class InputInterface
    {
        public virtual string GetNextInput()
        {
            return Console.ReadLine();
        }
    }
}
