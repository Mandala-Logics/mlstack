using System;
using System.Reflection;
using System.Threading;
using mlAutoCollection;

namespace mlThreadMGMT
{
    /// <summary>
    /// Represents a task that runs on a separate thread.
    /// </summary>
    public partial class ThreadTask : IEquatable<ThreadTask>, IDisposable
    {
        public static ThreadTask EmptyTask = new EmptyTask();

        // PRIVATE FIELDS
        internal Thread thread;
        private readonly int hash = CollectionHandle.Random.Next();

        // PROTECTED FIELDS
        protected volatile object ReturnValue;
        protected volatile bool ReturnedValue;

        // PUBLIC PROPERTIES

        /// <summary>
        /// Gets the delegate to be executed by the thread.
        /// </summary>
        public Delegate ThreadDelegate { get; }
        public WaitBase Waiter {get;}

        /// <summary>
        /// Gets the thread controller that monitors this task.
        /// </summary>
        public ThreadController ThreadHandle { get; }

        /// <summary>
        /// Gets a value indicating whether the thread has started.
        /// </summary>
        public bool ThreadStarted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the thread has finished running.
        /// </summary>
        public bool FinishedRunning => ThreadStarted && !thread.IsAlive;

        /// <summary>
        /// Gets a value indicating whether the thread is currently running.
        /// </summary>
        public bool ThreadRunning => thread.IsAlive;

        /// <summary>
        /// Gets or sets a value indicating whether the task should be removed on completion.
        /// </summary>
        public bool RemoveOnComplete { get; internal set; } = true;

        /// <summary>
        /// Gets or sets the priority of the thread task.
        /// </summary>
        public ThreadTaskPriority Priority { get; internal set; }

        /// <summary>
        /// Gets or sets the owner of the thread task.
        /// </summary>
        public ThreadManager Owner { get; internal set; } = null;

        // CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadTask"/> class with an action to execute.
        /// </summary>
        /// <param name="action">The action to execute on the thread.</param>
        public ThreadTask(Action<ThreadController> action) : this((Delegate)action) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadTask"/> class with an action to execute and a progress handler.
        /// </summary>
        /// <param name="action">The action to execute on the thread.</param>
        /// <param name="progressHandler">The progress handler for reporting progress.</param>
        public ThreadTask(Action<ThreadController> action, IProgress<long> progressHandler, CancellationToken cancellationToken = default)
            : this((Delegate)action, progressHandler, cancellationToken) { }

        internal ThreadTask(Delegate del)
        {
            thread = new Thread(ThreadLoop);
            ThreadHandle = new ThreadController(this);
            Waiter = new ThreadTaskWaiter(this);
            ThreadDelegate = del;
        }

        internal ThreadTask(Delegate del, IProgress<long> progressHandler)
        {
            thread = new Thread(ThreadLoop);
            ThreadHandle = new ThreadController(this, progressHandler, default);
            Waiter = new ThreadTaskWaiter(this);
            ThreadDelegate = del;
        }

        internal ThreadTask(Delegate del, IProgress<long> progressHandler, CancellationToken cancellationToken)
        {
            thread = new Thread(ThreadLoop);
            ThreadHandle = new ThreadController(this, progressHandler, cancellationToken);
            Waiter = new ThreadTaskWaiter(this);
            ThreadDelegate = del;
        }

        protected ThreadTask(ThreadTaskPriority priority) : this()
        {
            Priority = priority;
        }

        protected ThreadTask()
        {
            thread = new Thread(ThreadLoop);
            ThreadHandle = new ThreadController(this);
            Waiter = new ThreadTaskWaiter(this);
        }

        // PROTECTED ABSTRACT FUNCTIONS

        /// <summary>
        /// The main loop of the thread task, executed when the thread starts.
        /// </summary>
        protected virtual void ThreadLoop()
        {
            object ret = null;

            try { ret = ThreadDelegate.DynamicInvoke(ThreadHandle); }
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

        // PUBLIC FUNCTIONS

        /// <summary>
        /// Blocks the calling thread until the thread task terminates.
        /// </summary>
        public virtual void Join()
        {
            if (thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId) { throw new CantJoinOwnThreadException(this, "Not able to join the calling thread."); }

            thread.Join();

            if (ThreadHandle.HasException) throw ThreadHandle.ThreadException;
        }
        public virtual void Join(TimeSpan timeout)
        {
            if (thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId) { throw new CantJoinOwnThreadException(this, "Not able to join the calling thread."); }

            if (!thread.Join(timeout)) { throw new TimeoutException(); }

            if (ThreadHandle.HasException) throw ThreadHandle.ThreadException;
        }

        /// <summary>
        /// Starts the thread task.
        /// </summary>
        public void Start()
        {
            if (ThreadRunning) throw new ThreadStateException("Thread is already running.");

            if (FinishedRunning)
            {
                ThreadHandle.Reset();

                thread = new Thread(ThreadLoop);

                thread.Start();
            }          
            else
            {
                thread.Start();

                ThreadStarted = true;
            }
        }

        /// <summary>
        /// Blocks the calling thread until the thread task returns a value.
        /// </summary>
        /// <returns>The return value of the thread task.</returns>
        public virtual object AwaitReturnValue()
        {
            if(thread.IsAlive) { thread.Join(); }

            return GetReturnValue();
        }
        public virtual object AwaitReturnValue(TimeSpan timeout)
        {
            if(thread.IsAlive)
            {
                if (!thread.Join(timeout)) { throw new TimeoutException(); }
            }

            return GetReturnValue();
        }

        /// <summary>
        /// Blocks the calling thread until the thread task is aborted.
        /// </summary>
        public void AwaitAbort(bool force = false)
        {
            if (!thread.IsAlive) return;

            ThreadHandle.Abort();

            if (thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId) { throw new CantJoinOwnThreadException(this, "Not able to cleanly abort the calling thread."); }
            
            if (force)
            {
                try { thread.Abort(); }
                catch (PlatformNotSupportedException) { }
            }

            thread.Join();
        }
        public void AwaitAbort(TimeSpan timeout, bool force = false)
        {
            if (!thread.IsAlive) return;

            ThreadHandle.Abort();

            if (thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId) { throw new CantJoinOwnThreadException(this, "Not able to cleanly abort the calling thread."); }
            
            if (force)
            {
                try { thread.Abort(); }
                catch (PlatformNotSupportedException) { }
            }

            if (!thread.Join(timeout)) { throw new TimeoutException(); }
        }

        /// <summary>
        /// Aborts the thread task.
        /// </summary>
        public void Abort(bool force = false)
        {
            if (!thread.IsAlive) return;

            ThreadHandle.Abort();

            if (force)
            {
                try { thread.Abort(); }
                catch (PlatformNotSupportedException) { }
            }
        }

        /// <summary>
        /// Gets the return value of the thread task.
        /// </summary>
        /// <returns>The return value of the thread task.</returns>
        /// <exception cref="ThreadStateException">Thrown if the thread is not yet finished running.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the thread did not return a value.</exception>
        public virtual object GetReturnValue()
        {
            if (ThreadHandle.HasException) throw ThreadHandle.ThreadException;

            if (thread.IsAlive) throw new ThreadStateException("Thread is not yet finished running.");            

            if (!ReturnedValue)
            {
                if (ThreadHandle.Aborted) { throw new ThreadAbortedException(); }
                else { throw new InvalidOperationException($"This thread did not return a value."); }
            }

            Owner?.Completed(this);

            return ReturnValue;
        }

        // PRIVATE FUNCTIONS

        /// <summary>
        /// Marks the task as complete without a return value.
        /// </summary>
        protected void Complete()
        {  
            OnComplete();

            Owner?.Completed(this);
        }

        /// <summary>
        /// Marks the task as complete with a return value.
        /// </summary>
        /// <param name="ret">The return value.</param>
        protected void Complete(object ret)
        {  
            ReturnedValue = true;
            ReturnValue = ret;            

            OnComplete();
            OnComplete(ret);
        }

        // PROTECTED FUNCTIONS

        /// <summary>
        /// Called when the task is complete.
        /// </summary>
        protected virtual void OnComplete() { }

        /// <summary>
        /// Called when the task is complete with a return value.
        /// </summary>
        /// <param name="ret">The return value.</param>
        protected virtual void OnComplete(object ret) { }

        // OVERRIDES

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(obj as ThreadTask);

        /// <summary>
        /// Determines whether the specified <see cref="ThreadTask"/> is equal to the current <see cref="ThreadTask"/>.
        /// </summary>
        /// <param name="other">The <see cref="ThreadTask"/> to compare with the current <see cref="ThreadTask"/>.</param>
        /// <returns>true if the specified <see cref="ThreadTask"/> is equal to the current <see cref="ThreadTask"/>; otherwise, false.</returns>
        public bool Equals(ThreadTask other) => other != null && hash == other.hash;

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => hash;

        ~ThreadTask() => Dispose();

        public void Dispose()
        {
            Abort();
            GC.SuppressFinalize(this);
        }
    }
}