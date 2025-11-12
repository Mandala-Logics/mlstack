using System;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public partial class ThreadTask<retT> : ThreadTask
    {
        //PUBLIC PROPERTIES
        public new WaitBase<retT> Waiter {get;}

        //CONSTRCUTORS
        public ThreadTask(Func<ThreadController, retT> func) : base(func)
        {
            Waiter = new ThreadTaskWaiter(this);
        }
        public ThreadTask(Func<ThreadController, retT> func, IProgress<long> progressHandler) : base(func, progressHandler)
        {
            Waiter = new ThreadTaskWaiter(this);
        }
        public ThreadTask(Func<ThreadController, retT> func, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken)
        {
            Waiter = new ThreadTaskWaiter(this);
        }
        protected ThreadTask(Delegate del, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(del, progressHandler, cancellationToken)
        {
            Waiter = new ThreadTaskWaiter(this);
        }

        //PUBLIC METHODS
        public new retT GetReturnValue() => (retT)base.GetReturnValue();
        public new retT AwaitReturnValue() => (retT)base.AwaitReturnValue();
        public new retT AwaitReturnValue(TimeSpan timeout) => (retT)base.AwaitReturnValue(timeout);
    }

    public class ThreadTask<retT, T1> : ThreadTask<retT>
    {
        //PUBLIC PROPERTIES
        public object Arg {get;}

        //CONSTRCUTORS
        public ThreadTask(Func<ThreadController, T1, retT> func, object arg) : base(func, default, default)
        {
            Arg = arg;
        }
        public ThreadTask(Func<ThreadController, T1, retT> func, object arg, IProgress<long> progressHandler) : base(func, progressHandler, default)
        {
            Arg = arg;
        }
        public ThreadTask(Func<ThreadController, T1, retT> func, object arg, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken)
        {
            Arg = arg;
        }

        protected override void ThreadLoop()
        {
            object ret = null;

            try { ret = ThreadDelegate.DynamicInvoke(ThreadHandle, Arg); }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ThreadAbortedException)
                {
                    ThreadHandle.SetAborted();
                }
                else
                {
                    var ex = new ThreadException(this, e);

                    ThreadHandle.SetFailed(ex);
                }
            }

            if (ret is object) { Complete(ret); }
            else { Complete(); }
        }
    }

        public class ThreadTask<retT, T1, T2> : ThreadTask<retT>
    {
        //PUBLIC PROPERTIES
        public object[] Args {get;}

        //CONSTRCUTORS
        public ThreadTask(Func<ThreadController, T1, T2, retT> func, object[] args) : base(func, default, default)
        {
            if ((args?.Length ?? 0) != 2) { throw new ArgumentException($"Function requires two arguments, args array contains {args?.Length ?? 0} arguments. "); }

            Args = args;
        }
        public ThreadTask(Func<ThreadController, T1, T2, retT> func, object[] args, IProgress<long> progressHandler) : base(func, progressHandler, default)
        {
            if ((args?.Length ?? 0) != 2) { throw new ArgumentException($"Function requires two arguments, args array contains {args?.Length ?? 0} arguments. "); }

            Args = args;
        }
        public ThreadTask(Func<ThreadController, T1, T2, retT> func, object[] args, IProgress<long> progressHandler, CancellationToken cancellationToken) : base(func, progressHandler, cancellationToken)
        {
            if ((args?.Length ?? 0) != 2) { throw new ArgumentException($"Function requires two arguments, args array contains {args?.Length ?? 0} arguments. "); }

            Args = args;
        }

        protected override void ThreadLoop()
        {
            object ret = null;

            try { ret = ThreadDelegate.DynamicInvoke(ThreadHandle, Args); }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ThreadAbortedException)
                {
                    ThreadHandle.SetAborted();
                }
                else
                {
                    var ex = new ThreadException(this, e);

                    ThreadHandle.SetFailed(ex);
                }
            }

            if (ret is object) { Complete(ret); }
            else { Complete(); }
        }
    }
}
