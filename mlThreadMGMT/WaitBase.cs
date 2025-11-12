using System;
using System.Threading;
using System.Threading.Tasks;

namespace mlThreadMGMT
{
    public interface IWaitBase
    {
        bool IsCancelled {get;}
        bool IsCancelling {get;}
        bool HasException {get;}
        bool IsFailed {get;}
        bool IsRunning {get;}
        AggregateException Exception {get;}
        bool IsCompleted {get;}
        CancellationToken CancelToken {get;}
        object Result {get;}
        void ThrowIf();
        void Cancel();
        Task WaitAsync(CancellationToken cancellationToken);
        bool WaitOne(TimeSpan timeout);
        bool WaitOne();
        bool WaitOne(int millisecondsTimeout);
    }

    public abstract class WaitBase<T> : WaitHandle, IWaitBase
    {
        //STATIC FUNCTIONS
        public static WaitBase<T> CompletedWaiter = new EmptyWaitBase();

        //PUBLIC PROPERTIES
        public bool IsCancelled => TCS.Task.IsCanceled;
        public bool IsCancelling => CancelToken.IsCancellationRequested && !IsCancelled;
        public bool HasException => TCS.Task.IsFaulted;
        public bool IsFailed => HasException || IsCancelled;
        public bool IsRunning => !IsCompleted && !IsFailed;
        public AggregateException Exception => TCS.Task.Exception;
        public bool IsCompleted => TCS.Task.IsCompleted;
        public CancellationToken CancelToken {get;private set;}
        public T Result
        {
            get
            {
                WaitOne();
                return TCS.Task.Result;
            }
        }
        object IWaitBase.Result => Result;

        //PRIVATE PROPERTIES
        private readonly TaskCompletionSource<T> TCS = new TaskCompletionSource<T>();

        //CONSTRCUTORS
        protected WaitBase(CancellationToken cancellationToken = default)
        {
            CancelToken = cancellationToken; 
        }
        protected WaitBase(T result)
        {
            TCS.SetResult(result);
        }

        //PUBLIC METHODS
        public void ThrowIf()
        {
            if (HasException) { throw Exception; }
        }
        public void Cancel()
        {
            if (!IsRunning) { return; }

            if (!TCS.TrySetCanceled()) { return; }

            CancelToken = new CancellationToken(true);

            CancelWait();
        }

        //PROTECTED METHOD
        protected void SetException(Exception e)
        {
            if (e != null) TCS.TrySetException(e);
        }
        protected void ThorwIfCancelled()
        {
            if (IsCancelled) { throw new TaskCanceledException(); }
        }

        //OVERRIDES
        Task IWaitBase.WaitAsync(CancellationToken cancellationToken) => WaitAsync(cancellationToken);
        public Task<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (TCS.Task.IsCompleted) { return TCS.Task; }

            if (IsCancelling || cancellationToken.IsCancellationRequested)
            {
                Cancel();
                return TCS.Task;
            }

            new Thread(() => 
            { 
                WaitOne(TimeSpan.Zero);

            }).Start();

            return TCS.Task;
        }
        public override bool WaitOne()
            => WaitOne(TimeSpan.Zero);

        public override bool WaitOne(TimeSpan timeout)
        {
            if (!IsRunning) { return true; }

            if (IsCancelling)
            {
                Cancel();
                return false;
            }

            if (timeout.TotalMilliseconds < 0d) { timeout = TimeSpan.Zero; }

            try
            {
                var ret = DoWait(timeout);
                ThorwIfCancelled();
                TCS.TrySetResult(ret);
            }
            catch (TimeoutException e)
            {
                CancelWait();

                TCS.TrySetException(e);

                throw;
            }
            catch (OperationCanceledException)
            {
                CancelWait();

                TCS.TrySetCanceled();

                throw;
            }
            catch (Exception e)
            {
                CancelWait();

                TCS.TrySetException(e);

                throw;
            }

            if (Exception is object) { throw Exception.InnerException; }

            return IsCompleted;
        }
        public override bool WaitOne(int millisecondsTimeout)
            => WaitOne(TimeSpan.FromMilliseconds(millisecondsTimeout));

        protected override void Dispose(bool explicitDisposing)
        {
            Cancel();

            base.Dispose(explicitDisposing);
        }

        //ABSTRACT METHODS
        /// <summary>
        /// This indicates that the WaitBase will be disposed and cannot be called again, all resources must be disposed of at this point, all wait loops ended, etc.
        /// </summary>
        protected abstract void CancelWait();
        /// <summary>
        /// This function should perform any waiting and return a result value, or if there is no result, then it may return default.
        /// 
        /// This function may throw an OperationCancelledException via ThorwIfCancelled() (or manually), or it may throw a TimeoutException to indicate that the wait had timeoued, both of these will be dealt with by the base class and will be thrown back to the caller to inidcate failure.
        /// </summary>
        /// <param name="timeout">The time to wait; a timespan of TimeSpan.Zero means waiting indefinetly.</param>
        /// <returns>The retrun value, or if the wait does not return a value then just use default.</returns>
        protected abstract T DoWait(TimeSpan timeout);

        private sealed class EmptyWaitBase : WaitBase<T>
        {
            public EmptyWaitBase() : base(default(T)) {  }
            protected override void CancelWait() { }
            protected override T DoWait(TimeSpan timeout) => default;
        }
    }

    public abstract class WaitBase : WaitHandle, IWaitBase
    {
        //STATIC PROPERTIES
        public static WaitBase CompletedWaiter {get;} = new EmptyWaitBase();

        //PUBLIC PROPERTIES
        public bool IsCancelled => TCS.Task.IsCanceled;
        public bool IsCancelling => CancelToken.IsCancellationRequested && !IsCancelled;
        public bool HasException => TCS.Task.IsFaulted;
        public bool IsFailed => HasException || IsCancelled;
        public bool IsRunning => !IsCompleted && !IsFailed;
        public AggregateException Exception => TCS.Task.Exception;
        public bool IsCompleted => TCS.Task.IsCompleted;
        public CancellationToken CancelToken {get;private set;}
        public object Result
        {
            get
            {
                WaitOne();
                return TCS.Task.Result;
            }
        }

        //PRIVATE PROPERTIES
        private TaskCompletionSource<object> TCS {get; set;} = new TaskCompletionSource<object>();

        //CONSTRCUTORS
        protected WaitBase(CancellationToken cancellationToken = default)
        {
            CancelToken = cancellationToken; 
        }
        protected WaitBase(object result)
        {
            TCS.TrySetResult(result);
        }

        //PROTECTED METHODS
        protected void SetException(Exception e)
        {
            if (e != null) TCS.TrySetException(e);
        }
        protected void ThorwIfCancelled()
        {
            if (IsCancelled) { throw new TaskCanceledException(); }
        }

        //PUBLIC METHODS
        public void ThrowIf()
        {
            if (HasException) { throw Exception; }
        }
        public void Cancel()
        {
            if (!IsRunning) { return; }

            if (!TCS.TrySetCanceled()) { return; }

            CancelToken = new CancellationToken(true);

            CancelWait();
        }

        //OVERRIDES
        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (TCS.Task.IsCompleted) { return TCS.Task; }

            if (IsCancelling || cancellationToken.IsCancellationRequested)
            {
                Cancel();
                return TCS.Task;
            }

            new Thread(() => 
            { 
                WaitOne(TimeSpan.Zero);

            }).Start();

            return TCS.Task;
        }
        public override bool WaitOne()
            => WaitOne(TimeSpan.Zero);

        public override bool WaitOne(TimeSpan timeout)
        {
            if (!IsRunning) { return true; }

            if (IsCancelling)
            {
                Cancel();
                return false;
            }

            if (timeout.TotalMilliseconds < 0d) { timeout = TimeSpan.Zero; }

            try
            {
                var ret = DoWait(timeout);
                ThorwIfCancelled();
                TCS.TrySetResult(ret);
            }
            catch (TimeoutException e)
            {
                CancelWait();

                TCS.TrySetException(e);

                throw;
            }
            catch (OperationCanceledException)
            {
                CancelWait();

                TCS.TrySetCanceled();

                throw;
            }
            catch (Exception e)
            {
                CancelWait();

                TCS.TrySetException(e);
            }

            if (Exception is object) { throw Exception.InnerException; }

            return IsCompleted;
        }
        public override bool WaitOne(int millisecondsTimeout)
            => WaitOne(TimeSpan.FromMilliseconds(millisecondsTimeout));

        //ABSTRACT METHODS
        /// <summary>
        /// This indicates that the WaitBase will be disposed and cannot be called again, all resources must be disposed of at this point, all wait loops ended, etc.
        /// </summary>
        protected abstract void CancelWait();
        /// <summary>
        /// This function should perform any waiting and return a result value, or if there is no result, then it may return default.
        /// 
        /// This function may throw an OperationCancelledException via ThorwIfCancelled() (or manually), or it may throw a TimeoutException to indicate that the wait had timeoued, both of these will be dealt with by the base class and will be thrown back to the caller to inidcate failure.
        /// </summary>
        /// <param name="timeout">The time to wait; a timespan of TimeSpan.Zero means waiting indefinetly.</param>
        /// <returns>The retrun value, or if the wait does not return a value then just use default.</returns>
        protected abstract object DoWait(TimeSpan timeout);

        private sealed class EmptyWaitBase : WaitBase
        {
            public EmptyWaitBase() : base(new object()) {  }
            protected override void CancelWait() { }
            protected override object DoWait(TimeSpan timeout) => default;
        }
    }
}