using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public sealed class ThreadPool : ThreadTask
    {
        //PRIVATE STATIC PROPERTIES
        private static readonly Type WaiterType = typeof(ThreadPoolWaiter<>);

        //PUBLIC PROPERTIES        
        public int MaxThreads { get; }

        //PRIVATE PROPERTIES
        private bool joining = false;
        private ConcurrentQueue<IWaitBase> WorkQueue { get; } = new ConcurrentQueue<IWaitBase>();

        //CONSTRCUTORS
        public ThreadPool(int maxThreads = 10) : base(null)
        {
            if (maxThreads < 5) throw new ArgumentException("maxThreads is less than five.");
            else if (maxThreads > 25) maxThreads = 25;

            MaxThreads = maxThreads;
        }

        public ThreadPool(CancellationToken cancellationToken, int maxThreads = 10) : base(null, default, cancellationToken)
        {
            if (maxThreads < 5) throw new ArgumentException("maxThreads is less than five.");
            else if (maxThreads > 25) maxThreads = 25;

            MaxThreads = maxThreads;
        }

        //PUBLIC PROPERTIES
        public WaitBase AddWork(Action action)
        {
            var waiter = new ThreadPoolWaiter(action);

            WorkQueue.Enqueue(waiter);

            return waiter;
        }
        public WaitBase<retT> AddWork<retT>(Func<retT> func)
        {
            var waiter = new ThreadPoolWaiter<retT>(func);

            WorkQueue.Enqueue(waiter);

            return waiter;
        }

        //OVERRIDES
        protected override void ThreadLoop()
        {
            List<Thread> threads = new List<Thread>(5);
            int change = 0;
            int prev = 0;

            Thread thread;
            IWaitBase item;           

            while (!(joining && WorkQueue.Count == 0) && !ThreadHandle.Aborting)
            {
                while (WorkQueue.TryDequeue(out item)) 
                {
                    try { Invoke(item); }
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

                        foreach (Thread t in threads) { t.Abort(); }
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

                SpinWait.SpinUntil(() => !WorkQueue.IsEmpty || joining || ThreadHandle.Aborting);
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
            IWaitBase item = null;

            while (!WorkQueue.IsEmpty && !ThreadHandle.Aborting && !joining)
            {
                if (!SpinWait.SpinUntil(() => WorkQueue.TryDequeue(out item) || WorkQueue.IsEmpty, 5000))
                {
                    return;
                }

                try { Invoke(item); }
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
        private void Invoke(IWaitBase waiter)
        {
            if (waiter is ThreadPoolWaiter x)
            {
                x.Invoke();
            }
            else
            {
                waiter.GetType().GetMethod("Invoke").Invoke(waiter, null);
            }
        }

        //NESTED CLASSES
        private sealed class ThreadPoolWaiter : WaitBase
        {
            public static TimeSpan LoopWait {get;} = TimeSpan.FromMilliseconds(100);

            public Action Action {get;}
            private volatile object res;

            public ThreadPoolWaiter(Action action)
            {
                Action = action ?? throw new ArgumentNullException(nameof(action));
            }

            protected override void CancelWait() { }
            protected override object DoWait(TimeSpan timeout)
            {
                if (!SpinWait.SpinUntil(() => res is object || IsCancelling || IsCancelled, (int)(timeout == TimeSpan.Zero ? -1 : timeout.TotalMilliseconds)))
                {
                    throw new TimeoutException();
                }

                return res;
            }

            public void Invoke()
            {
                if (!IsRunning || IsCancelling) { return; }

                try
                {
                    Action.Invoke();
                    res = new object();
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            }
        }

        private sealed class ThreadPoolWaiter<T> : WaitBase<T>
        {
            public Func<T> Func {get;}
            private T res;
            private volatile bool complete;

            public ThreadPoolWaiter(Func<T> func)
            {
                Func = func ?? throw new ArgumentNullException(nameof(func));
            }

            protected override void CancelWait() { }
            protected override T DoWait(TimeSpan timeout)
            {
                if (!SpinWait.SpinUntil(() => complete || IsCancelling || IsCancelled, (int)(timeout == TimeSpan.Zero ? -1 : timeout.TotalMilliseconds)))
                {
                    throw new TimeoutException();
                }

                return res;
            }

            public void Invoke()
            {
                if (!IsRunning || IsCancelling) { return; }

                try
                {
                    res = Func.Invoke();
                    complete = true;
                }
                catch (Exception e)
                {
                    SetException(e);
                }
            }
        }
    }
}
