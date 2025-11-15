using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DDEncoder;

namespace mlFileInterface
{
    public enum TaskType { Null, Read, Write, Truncate, Encode, Decode, Scramble }

    public sealed class DecodeTask : IOTask
    {
        //STATIC PROPERTIES
        public new static readonly DecodeTask Completed = new DecodeTask();

        //PUBLIC PROPERTIES
        public override EncodedValue DecodedObject => WaitOnObject();
        internal bool ValueSet { get; private set; }

        //PRIVATE FIELDS
        private EncodedValue val;

        //CONSTRCUTROS
        private DecodeTask() : base()
        {
            val = default;
            ValueSet = true;
        }
        internal DecodeTask(IOStartPosition startPosition) : base(startPosition) { }

        //INTERNAL FUNCTIONS
        internal override void SetDecodedValue(EncodedValue value)
        {
            val = value;
            ValueSet = true;
        }

        //PUBLIC FUNCTION
        public override EncodedValue WaitOnObject()
        {
            Wait();

            return val;
        }
        public new async Task<EncodedValue> WaitOnObjectAsync()
        {
            await WaitAsync();

            return DecodedObject;
        }

        //PROTECTED FUNCTIONS
        public override void OnCancelled() { val = default; }
    }

    public class IOTask : IDisposable
    {
        //STATIC PROPERTIES
        public static readonly IOTask Completed = new IOTask();

        //PUBLIC PROPERTIES
        public TaskType Type { get; }
        public virtual EncodedValue DecodedObject => throw new InvalidOperationException("Cannot wait on object because this is not a decode task.");
        public long StartPosition => StartPosStruct.GetPosition();
        public bool HasStartPosition => StartPosition >= 0L;
        internal IOStartPosition StartPosStruct { get; private set; }
        internal bool Awaited { get; private set; } = false;
        public long EndPosition { get; private set; } = -1L;
        public bool HasEndPosition => EndPosition >= 0L;
        public long Length => EndPosition - StartPosition;
        internal Task<IOTask> Task { get; }
        public bool Success => Task.Status == TaskStatus.RanToCompletion;
        public bool HasException => Task.Exception is object;
        public Exception Exception => Task.Exception?.InnerExceptions.First() ?? default;
        public bool Finished => Task.IsCompleted || Task.IsFaulted || Task.IsCanceled;
        public byte[] Bytes
        {
            get
            {
                if (stream is object) { return stream.GetBuffer(); }
                else
                {
                    throw new InvalidOperationException("There is no buffer; this may be because the task failed or is not yet complete.");
                }
            }
        }
        public bool Cancelled => IsCancelled();        
        public CancellationToken CancelToken { get; internal set; }

        //PRIVATE PROPERTIES
        private TaskCompletionSource<IOTask> tcs;
        internal MemoryStream? stream = null;
        internal int readLength;

        //CONTRUCTORS
        protected internal IOTask()
        {
            tcs = new TaskCompletionSource<IOTask>();
            Task = tcs.Task;
            Type = TaskType.Null;

            stream = new MemoryStream();            

            tcs.SetResult(this);

            StartPosStruct = new IOStartPosition(StartPositionType.StartOfFile);
        }
        internal IOTask(IOStartPosition startPosition)
        {
            Type = TaskType.Decode;
            StartPosStruct = startPosition;

            tcs = new TaskCompletionSource<IOTask>();
            Task = tcs.Task;
        }
        internal IOTask(TaskType type, IOStartPosition startPosition)
        {
            Type = type;
            StartPosStruct = startPosition;

            stream = new MemoryStream();

            tcs = new TaskCompletionSource<IOTask>();
            Task = tcs.Task;
        }
        internal IOTask(TaskType type, IOStartPosition startPosition, int length) //for reading
        {
            Type = type;
            StartPosStruct = startPosition;

            readLength = length;

            tcs = new TaskCompletionSource<IOTask>();
            Task = tcs.Task;
        }        
        internal IOTask(TaskType type, IOStartPosition startPosition, byte[] buffer) //for writing
        {
            Type = type;
            StartPosStruct = startPosition;

            stream = new MemoryStream(buffer);

            tcs = new TaskCompletionSource<IOTask>();
            Task = tcs.Task;
        }

        //PUBLIC FUNCTIONS
        internal virtual void SetDecodedValue(EncodedValue value)
        {
            throw new InvalidOperationException("Wrong type of task.");
        }
        public IOTask Wait()
        {
            Awaited = true;

            if (Task.IsCanceled) throw new TaskCanceledException();
            else if (HasException) throw Exception;

            try { Task.Wait(); }
            catch (AggregateException e) { throw e.InnerException; }

            return this;
        }
        public IOTask Wait(TimeSpan timeout)
        {
            Awaited = true;

            if (Task.IsCanceled) throw new TaskCanceledException();
            else if (HasException) throw Exception;

            try { Task.Wait(timeout); }
            catch (AggregateException e) { throw e.InnerException; }

            return this;
        }
        public async Task<IOTask> WaitAsync()
        {
            Awaited = true;

            if (Task.IsCanceled) throw new TaskCanceledException();
            else if (HasException) throw Exception;

            try { await Task; }
            catch (AggregateException e) { throw e.InnerException; }

            return this;
        }
        public void Cancel()
        {
            if (!Finished)
            {
                if (tcs.TrySetCanceled())
                {
                    OnCancelled();
                }                           
            }
        } 
        public bool TryCancel()
        {
            if (!Finished)
            {
                if (tcs.TrySetCanceled())
                {
                    OnCancelled();

                    return true;
                }                           
            }

            return false;
        }       
        public int CopyTo(byte[] buffer, int offset)
        {
            Wait();

            stream.Position = 0;
            return stream.Read(buffer, offset, (int)stream.Length);
        }
        public virtual void Dispose()
        {
            stream = null;
        }
        public virtual EncodedValue WaitOnObject()
        {
            throw new InvalidOperationException("Cannot wait on object because this is not a decode task.");
        }
        public virtual Task<EncodedValue> WaitOnObjectAsync()
        {
            throw new InvalidOperationException("Cannot wait on object because this is not a decode task.");
        }

        //INTERNAL FUNCTIONS
        internal void SetException(Exception e)
        {   
            if (!Task.IsCanceled)
            {
                tcs.TrySetException(e);
            }

            if (!HasEndPosition) { EndPosition = StartPosition; }
        }
        internal void SetEndPosition(long endPos)
        {
            EndPosition = endPos;

            if (!Task.IsCanceled)
            {
                tcs.TrySetResult(this);
            }
        }
        internal bool IsCancelled()
        {
            if (Task.IsCanceled) return true;
            else if (CancelToken.IsCancellationRequested)
            {
                if (tcs.TrySetCanceled())
                {
                    OnCancelled();
                }               

                return true;
            }

            return Task.IsCompleted;
        }

        //PROTECTED METHODS
        public virtual void OnCancelled()
        {
            stream = null;
        }
    }
}
