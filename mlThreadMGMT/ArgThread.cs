using System;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public class ArgThread<inT> : ThreadTask
    {
        private readonly object argument;

        public ArgThread(Action<ThreadController, inT> func, inT loopArg) : base(func)
        {
            argument = loopArg ?? throw new ArgumentException(nameof(loopArg));
        }
        public ArgThread(Action<ThreadController, inT> func, inT loopArg, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken)
        {
            argument = loopArg ?? throw new ArgumentException(nameof(loopArg));
        }

        protected override void ThreadLoop()
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

            Complete();
        }
    }
}