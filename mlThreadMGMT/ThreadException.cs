using System.Reflection;
using System;

namespace mlThreadMGMT
{
    public class ThreadException : Exception
    {
        public ThreadException(ThreadTask thread, TargetInvocationException e) :
            base($"Thread: {thread.ThreadDelegate.Method}\nOriginal Type: {e.InnerException.GetType().Name}\nMessage: {e.InnerException.Message}\nStack Trace: {e}", e)
        { }

        public ThreadException(ThreadTask thread, string message) :
            base($"Thread: {thread.ThreadDelegate.Method}\nMessage: {message}")
        { }

        public ThreadException(ThreadTask thread, string message, Exception e) :
            base($"Thread: {thread.ThreadDelegate.Method}\nMessage: {message}", e)
        { }

        public ThreadException(ThreadTask thread, Exception e) :
            base($"Thread: {thread.ThreadDelegate.Method}", e)
        { }
    }

    public class FailedToJoinThreadException : ThreadException
    {
        public FailedToJoinThreadException(ThreadTask thread, TargetInvocationException e) : base(thread, e)
        {
        }

        public FailedToJoinThreadException(ThreadTask thread, string message) : base(thread, message)
        {
        }

        public FailedToJoinThreadException(ThreadTask thread, Exception e) : base(thread, e)
        {
        }

        public FailedToJoinThreadException(ThreadTask thread, string message, Exception e) : base(thread, message, e)
        {
        }
    }

    public class CantJoinOwnThreadException : ThreadException
    {
        public CantJoinOwnThreadException(ThreadTask thread, TargetInvocationException e) : base(thread, e)
        {
        }

        public CantJoinOwnThreadException(ThreadTask thread, string message) : base(thread, message)
        {
        }

        public CantJoinOwnThreadException(ThreadTask thread, Exception e) : base(thread, e)
        {
        }

        public CantJoinOwnThreadException(ThreadTask thread, string message, Exception e) : base(thread, message, e)
        {
        }
    }
}
