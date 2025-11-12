using System;
using System.Reflection;
using System.Threading;

namespace mlThreadMGMT
{
    public sealed class MinerThread : ThreadTask 
    {
        public TimeSpan LoopRest { get; }

        //CONSTRCUTORS
        public MinerThread(TimeSpan rest, Func<ThreadController, object> func) : base(func) 
        {
            if (rest.TotalMilliseconds <= 10d) throw new ArgumentException("Loop rest must be at least 10ms.");

            LoopRest = rest; 
        }
        public MinerThread(TimeSpan rest, Func<ThreadController, object> func, IProgress<long> progressHandler) : base(func, progressHandler) 
        {
            if (rest.TotalMilliseconds <= 10d) throw new ArgumentException("Loop rest must be at least 10ms.");

            LoopRest = rest; 
        }

        //OVERRIDES
        protected override void ThreadLoop()
        {   
            do
            {
                try
                {
                    ReturnValue = ThreadDelegate.DynamicInvoke(ThreadHandle);
                    ReturnedValue = true;
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

                Thread.Sleep(LoopRest);

            } while (!ThreadHandle.Aborting);

            Complete();
        }
        public override object GetReturnValue()
        {
            if (ThreadHandle.HasException) throw ThreadHandle.ThreadException;
            else if (!ReturnedValue) throw new InvalidOperationException("This thread has not returned a value yet.");             

            return ReturnValue;
        }
        public override object AwaitReturnValue()
        {
            if (ThreadHandle.HasException) { throw ThreadHandle.ThreadException; }
            else if (ReturnedValue) { return GetReturnValue(); }
            else
            {
                if (SpinWait.SpinUntil(() => ReturnedValue, 100))
                {
                    return GetReturnValue();
                }
                else
                {
                    throw new ThreadException(this, "Failed to retrive returned value before timeout.");
                }
            }
        }
    }
}
