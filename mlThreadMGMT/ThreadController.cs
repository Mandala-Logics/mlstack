using System;
using System.Threading;

namespace mlThreadMGMT
{
    public enum DDThreadState { Null = -1, Running = 0, Finished, Aborting, Aborted, Failed }

    public readonly struct ThreadStatus
    {
        public readonly string TaskType { get; }
        public ThreadTaskPriority Priority { get; }
        public DDThreadState State { get; }

        public ThreadStatus(ThreadController tc)
        {
            TaskType = tc.Task.GetType().Name;

            Priority = tc.Task.Priority;

            if (tc.Aborted) { State = DDThreadState.Aborted; }
            else if (tc.FinishedRunning) { State = DDThreadState.Finished; }
            else if (tc.Running) { State = DDThreadState.Running; }
            else if (tc.Aborting) { State = DDThreadState.Aborting; }
            else if (tc.Failed) { State = DDThreadState.Failed; }
            else { State = DDThreadState.Null; }
        }
    }

    /// <summary>
    /// Controls and monitors the execution of a thread task.
    /// </summary>
    public sealed class ThreadController
    {
        public static ThreadController NullController = ThreadTask.EmptyTask.ThreadHandle;

        // PUBLIC PROPERTIES

        /// <summary>
        /// Gets a value indicating whether the task has been aborted.
        /// </summary>
        public bool Aborted { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the task is in the process of aborting.
        /// </summary>
        public bool Aborting => (abort || CancellationToken.IsCancellationRequested) && !Aborted && Task.ThreadRunning;

        /// <summary>
        /// Gets the exception that occurred during the thread execution, if any.
        /// </summary>
        public Exception ThreadException { get; private set; } = null;

        /// <summary>
        /// Gets a value indicating whether an exception has occurred during the thread execution.
        /// </summary>
        public bool HasException { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the task has failed.
        /// </summary>
        public bool Failed { get; private set; } = false;

        /// <summary>
        /// Gets the associated thread task.
        /// </summary>
        public ThreadTask Task { get; }

        /// <summary>
        /// Gets a value indicating whether the task is currently running.
        /// </summary>
        public bool Running => Task.ThreadRunning;

        /// <summary>
        /// Gets a value indicating whether the task has finished running.
        /// </summary>
        public bool FinishedRunning => Task.FinishedRunning;

        /// <summary>
        /// Gets the progress reporter for the task.
        /// </summary>
        public IProgress<long> Progress { get; }
        public CancellationToken CancellationToken { get; }

        // PRIVATE PROPERTIES
        private volatile bool abort = false;

        // CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadController"/> class with a task and progress reporter.
        /// </summary>
        /// <param name="task">The associated thread task.</param>
        /// <param name="progress">The progress reporter.</param>
        internal ThreadController(ThreadTask task, IProgress<long> progress, CancellationToken cancellationToken)
        {
            Task = task;
            Progress = progress;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadController"/> class with a task.
        /// </summary>
        /// <param name="task">The associated thread task.</param>
        internal ThreadController(ThreadTask task)
        {
            Task = task;
        }

        // PUBLIC METHODS

        /// <summary>
        /// Requests the task to abort.
        /// </summary>
        public void Abort()
        {
            abort = true;
        }

        /// <summary>
        /// Throws a <see cref="ThreadAbortedException"/> if the task has been requested to abort.
        /// </summary>
        public void ThrowIfAborted() { if (Aborting) throw new ThreadAbortedException(); }

        /// <summary>
        /// Gets the current status of the thread task.
        /// </summary>
        /// <returns>A string representing the status of the thread task.</returns>
        public ThreadStatus GetStatus() => new ThreadStatus(this);

        // INTERNAL FUNCTIONS

        /// <summary>
        /// Resets the state of the thread controller.
        /// </summary>
        internal void Reset()
        {
            Aborted = false;
            ThreadException = null;
            HasException = false;
            Failed = false;
            abort = false;
        }

        /// <summary>
        /// Sets the exception that occurred during the thread execution.
        /// </summary>
        /// <param name="e">The exception to set.</param>
        internal void SetException(Exception e)
        {
            ThreadException = e;
            HasException = true;
        }

        /// <summary>
        /// Sets the task as failed and records the exception.
        /// </summary>
        /// <param name="e">The exception to record.</param>
        internal void SetFailed(Exception e)
        {
            ThreadException = e;
            HasException = true;
            Failed = true;
        }

        /// <summary>
        /// Sets the task as failed.
        /// </summary>
        internal void SetFailed() { Failed = true; }

        /// <summary>
        /// Sets the task as aborted.
        /// </summary>
        internal void SetAborted()
        {
            Failed = true;
            Aborted = true;
        }

        // STATIC OPERATORS

        /// <summary>
        /// Defines an implicit conversion of a <see cref="ThreadController"/> to a <see cref="bool"/> indicating whether an abort has been requested.
        /// </summary>
        /// <param name="tc">The thread controller.</param>
        public static implicit operator bool(ThreadController tc) => tc.abort;
    }
}