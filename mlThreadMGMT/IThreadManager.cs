using System;
using System.Collections.Generic;

namespace mlThreadMGMT
{
    public interface IThreadManager : IDisposable
    {
        int TotalThreads { get; }
        int PriorityCount(ThreadTaskPriority priority);
        List<ThreadStatus> GetThreadsStatus();
        bool Disposed { get; }
    }    
}
