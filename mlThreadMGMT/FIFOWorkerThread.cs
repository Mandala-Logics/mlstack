using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public sealed class FIFOWorkerThread<T> : ThreadTask
    {
        //PUBLIC PROPERTIES
        public ConcurrentQueue<T> WorkQueue { get; }
        public int MaxThreads { get; }
        public bool KeepAlive { get; }

        //PRIVATE PROPERTIES
        private bool joining = false;

        //CONSTRCUTORS
        public FIFOWorkerThread(Action<T> workAction, ConcurrentQueue<T> work, int maxThreads = 10, bool keepAlive = false) : base(workAction)
        {
            if (maxThreads < 0) throw new ArgumentException("maxThreads is less than zero.");
            else if (workAction is null) throw new ArgumentNullException("workAction");
            else if (work is null) throw new ArgumentException("work");
            else if (work.IsEmpty & !keepAlive) throw new ArgumentException("work bag is empty.");
            else if (maxThreads > 25) maxThreads = 25;

            WorkQueue = work;
            MaxThreads = maxThreads;
            KeepAlive = keepAlive;
        }

        public FIFOWorkerThread(Action<T> workAction, ConcurrentQueue<T> work, IProgress<long> progress, CancellationToken cancellationToken, int maxThreads = 10, bool keepAlive = false) : base(workAction, progress, cancellationToken)
        {
            if (maxThreads < 0) throw new ArgumentException("maxThreads is less than zero.");
            else if (workAction is null) throw new ArgumentNullException("workAction");
            else if (work is null) throw new ArgumentException("work");
            else if (work.IsEmpty & !keepAlive) throw new ArgumentException("work bag is empty.");
            else if (maxThreads > 25) maxThreads = 25;

            WorkQueue = work;
            MaxThreads = maxThreads;
            KeepAlive = keepAlive;
        }

        //OVERRIDES
        protected override void ThreadLoop()
        {
            if (WorkQueue.IsEmpty && !KeepAlive) return;

            List<Thread> threads = new List<Thread>(5);
            int change = 0;
            int prev = 0;

            Thread thread;
            T item;

            for (int x = 1; x <= 2; x++)
            {
                thread = new Thread(DoWork);
                thread.Start();
                threads.Add(thread);
            }            

            while ((!WorkQueue.IsEmpty || KeepAlive) && !joining)
            {
                while (WorkQueue.TryDequeue(out item)) 
                {
                    try { ThreadDelegate.DynamicInvoke(new object[] { item }); }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException is ThreadAbortedException)
                        {
                            ThreadHandle.SetAborted();
                        }
                        else
                        {
                            var ex = new ThreadException(this, e.InnerException);

                            ThreadHandle.SetFailed(ex);
                        }

                        return;
                    }

                    threads.RemoveAll((t) => t.ThreadState != ThreadState.Running);

                    if (ThreadHandle.Aborting)
                    {
                        foreach (Thread t in threads) { t.Abort(); }
                        ThreadHandle.SetAborted();
                        return;
                    }

                    if (threads.Count >= MaxThreads) continue;

                    change = WorkQueue.Count - prev;
                    prev = WorkQueue.Count;

                    if (change >= 0 && !WorkQueue.IsEmpty)
                    {
                        thread = new Thread(DoWork);
                        thread.Start();
                        threads.Add(thread);
                    }
                }

                if (KeepAlive) { SpinWait.SpinUntil(() => !WorkQueue.IsEmpty || joining); }
            }

            foreach (Thread t in threads) { t.Join(); }
            Complete();
        }
        public override void Join()
        {
            joining = true;

            base.Join();

            joining = false;
        }

        //PRIVATE METHODS
        private void DoWork()
        {
            T item = default;

            while (!WorkQueue.IsEmpty && !ThreadHandle.Aborting && !joining)
            {
                SpinWait.SpinUntil(() => WorkQueue.TryDequeue(out item) || WorkQueue.IsEmpty);

                if (WorkQueue.IsEmpty) { return; }

                try { ThreadDelegate.DynamicInvoke(new object[] { item }); }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException is ThreadAbortedException)
                    {
                        ThreadHandle.SetAborted();
                    }
                    else
                    {
                        var ex = new ThreadException(this, e.InnerException);

                        ThreadHandle.SetFailed(ex);
                    }

                    return;
                }
            }
        }

        //STATIC METHODS
        public static void WaitForWork(Action<T> workAction, ConcurrentBag<T> work, int maxThreads = 10)
        {
            WorkerThread<T> thread = new WorkerThread<T>(workAction, work, maxThreads);

            thread.Start();

            thread.Join();
        }
        public static WorkerThread<T> StartWork(Action<T> workAction, ConcurrentBag<T> work, int maxThreads = 10)
        {
            WorkerThread<T> thread = new WorkerThread<T>(workAction, work, maxThreads);

            thread.Start();

            return thread;
        }
    }
}
