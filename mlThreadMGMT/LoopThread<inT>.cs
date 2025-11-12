using System;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public sealed class LoopThread<inT> : ThreadTask 
    {
        //PRIVATE PROPERTIES
        private readonly object argument;

        //CONSTRCUTORS
        public LoopThread(Action<ThreadController, inT> func, inT loopArg) : base(func)
        {
            argument = loopArg ?? throw new ArgumentException(nameof(loopArg));
        }
        public LoopThread(Action<ThreadController, inT> func, inT loopArg, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken)
        {
            argument = loopArg ?? throw new ArgumentException(nameof(loopArg));
        }

        //PRIVATE PROPERTIES
        private SpinWait waiter = new SpinWait();

        //OVERRIDES
        protected override void ThreadLoop()
        {   
            do
            {
                try
                {
                    ThreadDelegate.DynamicInvoke(ThreadHandle, argument);
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
