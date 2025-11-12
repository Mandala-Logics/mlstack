using System;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using mlStringValidation.Path;
using DDEncoder;

namespace mlFileInterface
{
    public sealed class FileInterface : IDisposable
    {
        //PRIVATE FIELDS
        private Stream baseStream;
        private readonly Thread ioThread;
        private readonly ConcurrentQueue<IOTask> queue = new ConcurrentQueue<IOTask>();
        private volatile bool joinSignal = false;
        private volatile Exception threadException;
        private float completed = 0f;
        private int total = 0;

        //PUBLIC PROPERTIES
        public bool Disposed { get; private set; } = false;
        public long Length => baseStream.Length;
        public PathBase Path {get;}
        public float Progress {get;private set;} = 0f;

        //CONSTRUCTORS
        public FileInterface(PathBase path, FileMode fileMode)
        {
            baseStream = path.OpenStream(fileMode, FileAccess.ReadWrite, FileShare.ReadWrite);

            ioThread = new Thread(IOLoop);

            ioThread.Start();

            Path = path;
        }
        public FileInterface(Stream stream)
        {
            baseStream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (!stream.CanSeek) { throw new ArgumentException("Must be able to seek to use a stream in a fileinterface."); }

            ioThread = new Thread(IOLoop);

            ioThread.Start();

            Path = default;
        }
        ~FileInterface() => Dispose();

        //PUBLIC FUNCTIONS
        public void Dispose()
        {
            joinSignal = true;

            ioThread?.Join();

            baseStream?.Flush();

            baseStream?.Dispose();

            Disposed = true;

            GC.SuppressFinalize(this);
        }
        public void RestartThread()
        {
            joinSignal = true;

            ioThread.Join();

            joinSignal = false;

            ioThread.Start();

            threadException = null;           
        }
        public IOHandle GetHandle()
        {
            if (Disposed) throw new ObjectDisposedException("FileInterface");

            return new IOHandle(this);
        }
        public IOTask Write(SeekOrigin origin, long pos, byte[] buffer, int offset, int count)
        {
            IOStartPosition startPos;

            if (offset < 0) throw new ArgumentException("offset cannot be less than zero.");
            else if (count <= 0) throw new ArgumentException("Count cannot be less than or equal to zero.");
            else if (count + offset > buffer.Length) throw new ArgumentException($"End of read position ({count + offset}) is beyond the end of the buffer (buffer length: {buffer.Length}).");

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            var segment = new ArraySegment<byte>(buffer, offset, count);

            var ret = new IOTask(TaskType.Write, startPos, segment.Array);

            AddTask(ret);

            return ret;
        }
        public IOTask Encode(SeekOrigin origin, long pos, IConvertible obj)
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            EncodedValue ev;

            if (!DDEncoder.DDEncoder.TryCastToEncoded(obj, out ev))
            {
                throw new ArgumentException("Objects cannot be encoded unless they impliment IConvertable, IEncodable or IEnumberables thereof.");
            }

            var ret = new IOTask(TaskType.Encode, startPos);

            ret.stream.Write(ev.Type);

            ev.Write(ret.stream);

            AddTask(ret);

            return ret;
        }
        public IOTask Encode<T>(SeekOrigin origin, long pos, T[] obj) where T : IConvertible
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            EncodedValue ev;

            if (!DDEncoder.DDEncoder.TryCastToEncoded(obj, out ev))
            {
                throw new ArgumentException("Objects cannot be encoded unless they impliment IConvertable, IEncodable or IEnumberables thereof.");
            }

            var ret = new IOTask(TaskType.Encode, startPos);

            ret.stream.Write(ev.Type);

            ev.Write(ret.stream);

            AddTask(ret);

            return ret;
        }
        public IOTask Encode<T>(SeekOrigin origin, long pos, IEnumerable<IEncodable> obj) where T : IEncodable
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            EncodedValue ev;

            if (!DDEncoder.DDEncoder.TryCastToEncoded(obj, out ev))
            {
                throw new ArgumentException("Objects cannot be encoded unless they impliment IConvertable, IEncodable or IEnumberables thereof.");
            }

            var ret = new IOTask(TaskType.Encode, startPos);

            ret.stream.Write(ev.Type);

            ev.Write(ret.stream);

            AddTask(ret);

            return ret;
        }
        public IOTask Read(SeekOrigin origin, long pos, int readLength)
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            if (readLength <= 0) { throw new ArgumentException($"readLength is less than zero, value {readLength}."); }

            var ret = new IOTask(TaskType.Read, startPos, readLength);

            AddTask(ret);

            return ret;
        }
        public IOTask Encode(SeekOrigin origin, long pos, IEncodable obj)
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            EncodedValue ev = obj.GetEncodedObject();

            var ret = new IOTask(TaskType.Encode, startPos);

            ret.stream.Write(ev.Type);

            ev.Write(ret.stream);

            AddTask(ret);

            return ret;
        }
        public IOTask Decode(SeekOrigin origin, long pos, int readLength)
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            if (readLength <= 0) { throw new ArgumentException($"readLength is less than zero, value {readLength}."); }

            var ret = new DecodeTask(startPos);

            queue.Enqueue(ret);

            return ret;
        }

        public IOTask Scramble(SeekOrigin origin, long pos, int length) 
        {
            IOStartPosition startPos;

            switch (origin)
            {                
                case SeekOrigin.Begin:
                    startPos = new IOStartPosition(StartPositionType.StartOfFile, pos);
                    break;
                case SeekOrigin.End:
                    startPos = new IOStartPosition(StartPositionType.EndOfFile, pos);
                    break;
                case SeekOrigin.Current:
                default:
                    throw new ArgumentException("Invalid seek origin: " + origin);
            }

            return new IOTask(TaskType.Scramble, startPos, length);
        }

        //INTERNAL FUNCTIONS
        internal void AddTask(IOTask task)
        {
            if (Disposed) throw new ObjectDisposedException("FileInterface");

            queue.Enqueue(task);
            total++;

            if (!ioThread.IsAlive)
            {
                if (threadException is object)
                {
                    var e = threadException;
                    threadException = null;

                    throw e;
                }
                else
                {
                    throw new Exception("Thread failed, reason unknown.");
                }
                
            }
        }

        //PRIVATE FUNCTIONS
        private void IOLoop()
        {
            int tries;
            IOTask task;
            EncodedType et;
            EncodedValue ev;
            byte[] buffer;
            int len, c, n;
            SpinWait waiter = new SpinWait();

            do
            {
                while (queue.Count > 0)
                {
                    tries = -1;

                    do
                    {
                        tries++;

                        if (tries > 10)
                        {
                            threadException = new Exception("Failed to dequeue next task after 10 tries.");
                            return;
                        }

                    } while (!queue.TryDequeue(out task));

                    completed++;
                    Progress = completed/total;

                    if (task.IsCancelled())
                    {
                        task.stream?.Dispose();                        
                        continue;
                    }

                    try
                    {
                        if (!task.StartPosStruct.SetPosition(ref baseStream))
                        {
                            task.Cancel();
                            continue;
                        }

                        switch (task.Type)
                        {
                            case TaskType.Read:

                                buffer = new byte[task.readLength];

                                len = c = 0;

                                do
                                {
                                    if (c >= 10) break;

                                    len += baseStream.Read(buffer, len, buffer.Length - len);

                                    c++;

                                } while (len < buffer.Length);                          

                                task.stream = new MemoryStream(buffer, 0, len, false, true);

                                break;

                            case TaskType.Encode:
                            case TaskType.Write:

                                try
                                {
                                    task.stream.Position = 0L;
                                    task.stream.CopyTo(baseStream);
                                }
                                catch (ObjectDisposedException) { }
                                
                                break;
                            
                            case TaskType.Scramble:
                            
                                buffer = DDHash.GetRandomBytes(Math.Min(256, task.readLength));

                                n = 0;

                                while (n < task.readLength)
                                {
                                    c = Math.Min(buffer.Length, task.readLength - n);

                                    baseStream.Write(buffer, 0, c);

                                    unchecked
                                    {
                                        if ((n += c) < task.readLength)
                                        {
                                            DDHash.Shuffle(buffer);
                                        }
                                    }
                                }

                                break;

                            case TaskType.Truncate:

                                baseStream.SetLength(baseStream.Position);
                                break;

                            case TaskType.Decode:

                                baseStream.ReadEncodedType(out et);

                                if (et == EncodedType.Object)
                                {
                                    EncodedObject.Read(baseStream, out ev);
                                }
                                else if (et == EncodedType.Array)
                                {
                                    EncodedArray.Read(baseStream, out ev);
                                }
                                else
                                {
                                    EncodedValue.Read(baseStream, et, out ev);
                                }

                                task.SetDecodedValue(ev);

                                break;
                        }
                    }
                    catch (Exception e) { task.SetException(e); continue; }

                    task.SetEndPosition(baseStream.Position);

                    try { task.stream?.Seek(0L, SeekOrigin.Begin); }
                    catch (ObjectDisposedException) { }
                }

                baseStream.Flush();
                waiter.SpinOnce();

            } while (!(joinSignal && queue.Count == 0));
        }
    }
}
