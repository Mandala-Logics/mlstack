using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DDEncoder;

namespace mlFileInterface
{
    public sealed class IOHandle : IReadOnlyCollection<IOTask>, IDisposable
    {
        //PUBLIC PROPERTIES
        public long Position
        {
            get
            {
                if (nextStartPos.Type == StartPositionType.EndOfFile) return -1;

                long pos = nextStartPos.GetPosition();

                if (pos > -1L) { return pos; }
                else
                {
                    return nextStartPos.Previous.Wait().EndPosition;
                }
            }
            set => nextStartPos = new IOStartPosition(value);
        }
        public int Count => tasks.Count;
        public bool Empty => tasks.Count == 0;
        public FileInterface Owner { get; }
        public int ThreadID {get;}

        //PRIVATE FIELDS
        private readonly Queue<IOTask> tasks;
        private IOStartPosition nextStartPos;
        
        //CONSTRCUTORS
        internal IOHandle(FileInterface owner, long startPosition)
        {
            tasks = new Queue<IOTask>();
            Owner = owner;

            ThreadID = Thread.CurrentThread.ManagedThreadId;

            nextStartPos = new IOStartPosition(startPosition);
        }
        internal IOHandle(FileInterface owner)
        {
            tasks = new Queue<IOTask>();
            Owner = owner;

            ThreadID = Thread.CurrentThread.ManagedThreadId;

            nextStartPos = new IOStartPosition(StartPositionType.StartOfFile);
        }

        //PUBLIC METHODS
        public IOTask Next()
        {
            IOTask task;

            do
            {
                if (Count == 0) throw new InvalidOperationException("No more tasks to wait on.");

                task = tasks.Dequeue();

                if (task.Awaited) task.Dispose();

            } while (task.Awaited);

            return task;
        }
        public IOTask WaitNext()
        {
            var t = Next().Wait();

            if (t.HasException) throw t.Exception;
            else return t;
        }
        public async Task<IOTask> WaitNextAsync()
        {
            IOTask t;

            t = await Next().WaitAsync();

            if (t.HasException) throw t.Exception;
            else return t;
        }
        public void WaitAll()
        {
            if (Count == 0) { throw new InvalidOperationException("No more tasks to wait on."); }
            else
            {
                IOTask t;

                while (Count > 0)
                {
                    t = tasks.Dequeue().Wait();

                    if (t.HasException) throw t.Exception;
                }
            }
        }
        public async Task WaitAllAsync()
        {
            if (Count == 0) { throw new InvalidOperationException("No more tasks to wait on."); }
            else
            {
                IOTask t;

                while (Count > 0)
                {
                    t = await tasks.Dequeue().Task;

                    if (t.HasException) throw t.Exception;
                }
            }
        }
        public void Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    nextStartPos = new IOStartPosition(StartPositionType.StartOfFile, offset);
                    break;
                case SeekOrigin.Current:
                    nextStartPos = new IOStartPosition(Position + offset);
                    break;
                case SeekOrigin.End:
                    nextStartPos = new IOStartPosition(StartPositionType.EndOfFile, offset);
                    break;
            }
        }
        public void SeekToEnd()
        {
           nextStartPos = new IOStartPosition(StartPositionType.EndOfFile);
        }
        public void SeekToStart()
        {
            nextStartPos = new IOStartPosition(StartPositionType.StartOfFile);
        }
        public void Clear() => tasks.Clear();

        //PRIVATE FUNCTIONS
        private void QueueTask(IOTask task)
        {
            if (Thread.CurrentThread.ManagedThreadId != ThreadID)
                { throw new InvalidOperationException("IOHandle must be used on the same thread on which it was created, please create a new handle for each thread."); }

            tasks.Enqueue(task);
            Owner.AddTask(task);
        }

        //FACTORY FUNCTIONS
        public IOTask Scramble(int length, CancellationToken token = default)
        {
            if (length <= 0) throw new ArgumentException("Length cannot be less than or equal to zero.");

            var task = new IOTask(TaskType.Scramble, nextStartPos, length) { CancelToken = token };

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Read(int length, CancellationToken token = default)
        {
            if (length <= 0) throw new ArgumentException("Length cannot be less than or equal to zero.");

            var task = new IOTask(TaskType.Read, nextStartPos, length) { CancelToken = token };

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Write(byte[] buffer, int startIndex, int count, CancellationToken token = default)
        {
            if (startIndex < 0) throw new ArgumentException("StartIndex cannot be less than zero.");
            else if (count <= 0) throw new ArgumentException("Count cannot be less than or equal to zero.");
            else if (count + startIndex > buffer.Length) throw new ArgumentException($"End of read position ({count + startIndex}) is beyond the end of the buffer (buffer length: {buffer.Length}).");

            byte[] b = new byte[count];

            Buffer.BlockCopy(buffer, startIndex, b, 0, count);

            var task = new IOTask(TaskType.Write, nextStartPos, b) { CancelToken = token };

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Truncate(long value, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Truncate, new IOStartPosition(value)) { CancelToken = token };

            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEncodable obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Object);

            EncodedObject.GetEncodedObject(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(EncodedValue ev, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(ev.Type);

            ev.Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(int obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Int32);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(bool obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Boolean);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(byte obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Byte);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(string obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.String);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(char obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Char);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(DateTime obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.DateTime);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(decimal obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Decimal);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(double obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Double);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(short obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Int16);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(long obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Int64);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(sbyte obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.SByte);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(float obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Single);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(ushort obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.UInt16);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(uint obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.UInt32);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(ulong obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.UInt64);

            new EncodedValue(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode<T>(IEnumerable<T> obj, CancellationToken token = default) where T : IEncodable
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            EncodedArray.GetEncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode<T>(T[] obj, CancellationToken token = default) where T : IConvertible
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            EncodedArray.GetEncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<int> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<byte> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<long> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<short> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<uint> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<ulong> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<ushort> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<sbyte> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<float> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<double> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<decimal> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<DateTime> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<string> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<bool> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Encode(IEnumerable<char> obj, CancellationToken token = default)
        {
            var task = new IOTask(TaskType.Encode, nextStartPos) { CancelToken = token };

            task.stream.Write(EncodedType.Array);

            new EncodedArray(obj).Write(task.stream);

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }
        public IOTask Decode(CancellationToken token = default)
        {
            var task = new DecodeTask(nextStartPos) { CancelToken = token };

            nextStartPos = new IOStartPosition(task);
            QueueTask(task);

            return task;
        }

        //INTERFACE METHODS
        IEnumerator<IOTask> IEnumerable<IOTask>.GetEnumerator() => tasks.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => tasks.GetEnumerator();
        public void Dispose()
        {
            tasks?.Clear();
        }
    }
}
