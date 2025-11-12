using System;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public sealed class LoopThread : ThreadTask 
    {
        //CONSTRCUTORS
        public LoopThread(Action<ThreadController> func) : base(func) { }
        public LoopThread(Action<ThreadController> func, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken) { }

        //PRIVATE PROPERTIES
        private SpinWait waiter = new SpinWait();

        //OVERRIDES
        protected override void ThreadLoop()
        {   
            do
            {
                try
                {
                    ThreadDelegate.DynamicInvoke(ThreadHandle);
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException is ThreadAbortedException tae)
                    {
                        ThreadHandle.SetAborted();
                        return;
                    }

                    var ex = new ThreadException(this, e);

                    ThreadHandle.SetFailed(ex);
                    return;
                }
                catch (Exception e)
                {
                    var ex = new ThreadException(this, e);

                    ThreadHandle.SetFailed(ex);
                    return;
                }

                waiter.SpinOnce();

            } while (!ThreadHandle.Aborting);

            Complete();
        }
    }
}
