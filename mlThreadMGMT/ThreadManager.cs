using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using mlAutoCollection.Sync;

namespace mlThreadMGMT
{
    public enum ThreadTaskPriority : int { NonEssential = 0, Important = 1, MustBeCompleted = 2, All }
    public delegate void ThreadCompletedEventHandler(IThreadManager threadManager, ThreadTask thread);
    public delegate void ThreadExceptionEventHandler(IThreadManager threadManager, ThreadTask thread, Exception exception);
    public delegate void ThreadedEventHandler(object sender, ThreadController tc);

    public sealed class ThreadManager : IReadOnlyList<ThreadTask>, IThreadManager
    {
        //EVENTS
        public event ThreadCompletedEventHandler ThreadCompleted;
        public event ThreadExceptionEventHandler ThreadException;

        //PUBLIC PROPERTIES
        public bool RemoveOnCompleted { get; set; } = true;
        public int Count
        {
            get
            {
                ThrowIf();

                return list.Count;
            }
        }
        int IThreadManager.TotalThreads => Count;
        public ThreadTask this[int index]
        {
            get
            {
                ThrowIf();

                return list[index];
            }
        }
        public bool Disposed { get; private set; } = false;

        //PRIVATE PROPERTIES
        private readonly SyncedList<ThreadTask> list = new SyncedList<ThreadTask>();
        private readonly ConcurrentBag<ThreadTask> callbackQueue = new ConcurrentBag<ThreadTask>();  
        private readonly ThreadTask callbackThread;

        //CONSTRCUTORS
        public ThreadManager()
        {
            Add(callbackThread = new WorkerThread<ThreadTask>(ProcessCallback, callbackQueue, keepAlive: true), ThreadTaskPriority.Important);

            callbackThread.Start();
        }

        //PRIVATE METHODS
        private void ProcessCallback(ThreadTask task)
        {
            if (task.ThreadHandle.HasException)
            {
                ThreadException?.Invoke(this, task, task.ThreadHandle.ThreadException);
            }
            else
            {
                ThreadCompleted?.Invoke(this, task);
            }
        }

        //PUBLIC METHODS
        public void Add(ThreadTask threadTask)
        {
            if (threadTask is null) throw new ArgumentNullException("threadTask");
            else if (list.Contains(threadTask)) throw new ArgumentException("This thread has already been added to this manager.");
            else if (threadTask.Owner is ThreadManager && !threadTask.Owner.Equals(this)) throw new ArgumentException("This thread is already managed by another class.");

            list.Add(threadTask);
            threadTask.Owner = this;
            threadTask.Priority = ThreadTaskPriority.NonEssential;
        }
        public void Add(ThreadTask threadTask, ThreadTaskPriority priority)
        {
            if (threadTask is null) throw new ArgumentNullException("threadTask");
            else if (list.Contains(threadTask)) throw new ArgumentException("This thread has already been added to this manager.");
            else if (threadTask.Owner is ThreadManager && !threadTask.Owner.Equals(this)) throw new ArgumentException("This thread is already managed by another class.");

            list.Add(threadTask);
            threadTask.Owner = this;
            threadTask.Priority = priority;
        }
        public MinerThread CreateMiner(TimeSpan rest, Func<ThreadController, object> func)
        {
            ThrowIf();

            var ret = new MinerThread(rest, func) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = ThreadTaskPriority.NonEssential };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public MinerThread CreateMiner(TimeSpan rest, Func<ThreadController, object> func, IProgress<long> progressHandler)
        {
            ThrowIf();

            var ret = new MinerThread(rest, func, progressHandler) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = ThreadTaskPriority.NonEssential };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public LoopThread CreateLoop(Action<ThreadController> func)
        {
            ThrowIf();

            var ret = new LoopThread(func) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = ThreadTaskPriority.NonEssential };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public LoopThread CreateLoop(Action<ThreadController> func, IProgress<long> progressHandler)
        {
            ThrowIf();

            var ret = new LoopThread(func, progressHandler, default) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = ThreadTaskPriority.NonEssential };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public ThreadTask CreateTask(Action<ThreadController> action, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask(action) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            list.Add(ret);

            return ret;
        }
        public ThreadTask CreateTask(Action<ThreadController> action, IProgress<long> progressHandler, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask(action, progressHandler) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            list.Add(ret);

            return ret;
        }
        public ThreadTask<returnType> CreateTask<returnType>(Func<ThreadController, returnType> func, IProgress<long> progressHandler, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask<returnType>(func, progressHandler) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            list.Add(ret);

            return ret;
        }
        public ThreadTask<returnType> CreateTask<returnType>(Func<ThreadController, returnType> func, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask<returnType>(func) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            list.Add(ret);

            return ret;
        }
        public ThreadTask StartTask(Action<ThreadController> action, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask(action) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public ThreadTask StartTask(Action<ThreadController> action, IProgress<long> progressHandler, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask(action, progressHandler) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public ThreadTask<returnType> StartTask<returnType>(Func<ThreadController, returnType> func, IProgress<long> progressHandler, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask<returnType>(func, progressHandler) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public ThreadTask<returnType> StartTask<returnType>(Func<ThreadController, returnType> func, ThreadTaskPriority priority = 0)
        {
            ThrowIf();

            var ret = new ThreadTask<returnType>(func) { Owner = this, RemoveOnComplete = RemoveOnCompleted, Priority = priority };

            ret.Start();
            list.Add(ret);

            return ret;
        }
        public void AbortAll()
        {
            ThrowIf();

            foreach (ThreadTask task in list)
            {
                task.Abort();
            }
        }
        public void AbortAll(ThreadTaskPriority priority)
        {
            ThrowIf();

            foreach (ThreadTask task in list)
            {
                if (task.Priority == priority)
                {
                    task.Abort();
                }
            }
        }
        public void AbortAbove(ThreadTaskPriority priority)
        {
            ThrowIf();

            foreach (ThreadTask task in list)
            {
                if (task.Priority > priority)
                {
                    task.Abort();
                }
            }
        }
        public void AbortBelow(ThreadTaskPriority priority)
        {
            ThrowIf();

            foreach (ThreadTask task in list)
            {
                if (task.Priority < priority)
                {
                    task.Abort();
                }
            }
        }
        public void JoinAll()
        {
            ThrowIf();

            while (list.Count > 0)
            {
                foreach (ThreadTask task in list)
                {
                    task.Join();
                }
            }
        }
        public void StartAll()
        {
            ThrowIf();

            foreach (ThreadTask task in list)
            {
                try
                {
                    task.Start();
                }
                catch (ThreadStateException) { }
            }

        }
        public Exception? Run()
        {
            bool taskRunning = true;

            while (list.Count > 0 && taskRunning)
            {
                taskRunning = false;

                foreach (ThreadTask task in list)
                {
                    if (task.ThreadHandle.HasException)
                    {
                        list.Remove(task);
                        return task.ThreadHandle.ThreadException;
                    }

                    if (!taskRunning) { taskRunning = task.ThreadRunning; }
                }
            }

            return null;
        }
        public void ThrowIf()
        {
            if (Disposed) throw new ObjectDisposedException("ThreadManager");
            else if (callbackThread.ThreadHandle.HasException)
            {
                throw callbackThread.ThreadHandle.ThreadException;
            }
        }
        public void Dispose()
        {
            Disposed = true;

            foreach (ThreadTask task in list)
            {
                if (task.Priority < ThreadTaskPriority.MustBeCompleted)
                {
                    task.Abort();
                }
            }

            foreach (ThreadTask task in list)
            {
                try
                {
                    task.Join(TimeSpan.FromSeconds(30));
                }
                catch (TimeoutException)
                {
                    try { task.AwaitAbort(TimeSpan.FromSeconds(30), true); }
                    catch (CantJoinOwnThreadException) { }
                }
                catch (CantJoinOwnThreadException) { }
            }
        }
        public List<ThreadStatus> GetThreadsStatus()
        {
            var ret = new List<ThreadStatus>(list.Count);

            foreach (ThreadTask thread in list)
            {
                ret.Add(thread.ThreadHandle.GetStatus());
            }

            return ret;
        }

        //INTERNAL METHODS
        internal void Completed(ThreadTask task)
        {
            if (task == callbackThread)
            {
                return;
            }
            else
            {
                if (task.RemoveOnComplete) list.Remove(task);

                callbackQueue.Add(task);
            }            
        }

        //INTERFACE METHODS
        public IEnumerator<ThreadTask> GetEnumerator() => ((IReadOnlyList<ThreadTask>)list).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IReadOnlyList<ThreadTask>)list).GetEnumerator();
        int IThreadManager.PriorityCount(ThreadTaskPriority priority)
        {
            int ret = 0;

            foreach (ThreadTask task in list)
            {
                if (task.Priority == priority) ret++;
            }

            return ret;
        }
    }
}
