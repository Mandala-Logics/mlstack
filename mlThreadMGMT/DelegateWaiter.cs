using System;

namespace mlThreadMGMT
{
    public sealed class DelegateWaiter<T> : WaitBase<T>
    {
        //PUBLIC PROPERTIES
        public Func<T> Func {get;}

        //PRIVATE PROEPRTIES
        private T ret;
        private readonly ThreadTask thread;

        //CONSTRCUTORS
        public DelegateWaiter(Func<T> func)
        {
            Func = func;
            thread = new ThreadTask(Del, default);
            thread.Start();
        }

        //PRIVATE METHODS
        private void Del(ThreadController tc)
        {
            tc.ThrowIfAborted();

            if (IsCancelling) { return; }

            ret = Func.Invoke();
        }

        //OVERRIDES
        protected override void CancelWait()
        {
            thread.AwaitAbort();
        }
        protected override T DoWait(TimeSpan timeout)
        {
            if (!thread.Waiter.WaitOne(timeout))
            {
                thread.Abort(true);
                throw new TimeoutException();
            }

            return thread.ThreadHandle.Running ? throw new TimeoutException() : ret;
        }
    }

    public sealed class DelegateWaiter : WaitBase
    {
        //PUBLIC PROPERTIES
        public Action Func {get;}

        //PRIVATE PROEPRTIES
        private readonly ThreadTask thread;

        //CONSTRCUTORS
        public DelegateWaiter(Action func)
        {
            Func = func;
            thread = new ThreadTask(Del, default);
            thread.Start();
        }

        //PRIVATE METHODS
        private void Del(ThreadController tc)
        {
            tc.ThrowIfAborted();

            if (IsCancelling) { return; }

            Func.Invoke();
        }

        //OVERRIDES
        protected override void CancelWait()
        {
            thread.AwaitAbort();
        }
        protected override object DoWait(TimeSpan timeout)
        {
            if (!thread.Waiter.WaitOne(timeout))
            {
                thread.Abort(true);
            }

            return thread.ThreadHandle.Running ? throw new TimeoutException() : new object();
        }
    }
}