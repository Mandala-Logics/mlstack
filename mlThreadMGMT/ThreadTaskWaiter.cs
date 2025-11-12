using System;

namespace mlThreadMGMT
{
    public partial class ThreadTask
    {
        protected class ThreadTaskWaiter : WaitBase
        {
            private ThreadTask Owner {get;}

            public ThreadTaskWaiter(ThreadTask owner) { Owner = owner; }
            protected override void CancelWait()
            {
                Owner.Abort(true);
            }
            protected override object DoWait(TimeSpan timeout)
            {
                ThorwIfCancelled();

                if (timeout.Equals(TimeSpan.Zero)) { Owner.Join(); }
                else
                {
                    Owner.Join(timeout);
                }

                return Owner.ThreadHandle.Running ? throw new TimeoutException() : new object();
            }
        }
    }

    public partial class ThreadTask<retT>
    {
        protected new class ThreadTaskWaiter : WaitBase<retT>
        {
            private ThreadTask<retT> Owner {get;}

            public ThreadTaskWaiter(ThreadTask<retT> owner) { Owner = owner; }
            protected override void CancelWait()
            {
                Owner.Abort(true);
            }
            protected override retT DoWait(TimeSpan timeout)
            {
                if (timeout.Equals(TimeSpan.Zero)) { return Owner.AwaitReturnValue(); }
                else { return Owner.AwaitReturnValue(timeout); }
            }
        }
    }
}