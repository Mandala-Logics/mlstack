using System;

namespace mlThreadMGMT
{
    public sealed class ThreadAbortedException : Exception
    {
        public ThreadAbortedException() : base() { }
        public ThreadAbortedException(string message) : base(message) { }
    }
}
