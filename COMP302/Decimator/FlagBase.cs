namespace COMP302.Decimator
{
    public class FlagBase
    {
        protected int _flags = 0;

        public void SetFlags(int flags)
        {
            _flags = flags;
        }

        public void ClearFlags()
        {
            _flags = 0;
        }

        public bool HasFlag(int flag)
        {
            return (_flags & flag) == flag;
        }

        public void AddFlag(int flag)
        {
            _flags |= flag;
        }

        public void RemoveFlag(int flag)
        {
            _flags &= ~flag;
        }
    }
}
