using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace mlFileInterface
{
    public sealed class FileInterfaceStream : Stream
    {
        //PUBLIC PROPERTIES
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => fileInterface.Length;
        public override long Position { get => handle.Position; set => handle.Position = value; }

        //PRIVATE PROPERTIES
        private readonly IOHandle handle;
        private readonly FileInterface fileInterface;

        //CONSTRCUTORS
        public FileInterfaceStream(FileInterface fileInterface)
        {
            this.fileInterface = fileInterface ?? throw new ArgumentNullException(nameof(fileInterface));

            handle = fileInterface.GetHandle();
        }

        //PUBLIC METHODS
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return 0; }

            if (fileInterface.Disposed) { throw new ObjectDisposedException("Base FileInterface is disposed."); }

            IOTask task;

            lock (handle)
            {
                handle.Read(count);
    
                task = handle.Next().Wait();
            }

            task.Bytes.CopyTo(buffer, offset);

            return (int)task.Length;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return 0; }

            if (fileInterface.Disposed) { throw new ObjectDisposedException("Base FileInterface is disposed."); }

            IOTask task;

            lock (handle)
            {
                handle.Read(count);
    
                task = handle.Next();
            }

            task = await task.WaitAsync();

            task.Bytes.CopyTo(buffer, offset);

            return (int)task.Length;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (fileInterface.Disposed) { throw new ObjectDisposedException("Base FileInterface is disposed."); }
            else if (origin == SeekOrigin.Begin && offset < 0) { throw new ArgumentException("Offset cannot be less than zero while origin is set to Begin."); }

            lock (handle)
            {
                switch (origin)
                {               
                    case SeekOrigin.Current:
                        return handle.Position += offset;
                    case SeekOrigin.End:
                        return handle.Position = fileInterface.Length + offset;
                    case SeekOrigin.Begin:
                    default:
                        return handle.Position = offset;
                }
            }
        }
        public override void SetLength(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException($"Stream length ({value}) cannot be less than zero.");
            else if (fileInterface.Length <= value) { return; }

            lock (handle)
            {
                handle.Truncate(value);
                handle.Next().Wait();
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return; }

            if (fileInterface.Disposed) { throw new ObjectDisposedException("Base FileInterface is disposed."); }

            lock (handle)
            {
                handle.Write(buffer, offset, count);
                handle.Next().Wait();
            }
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return; }

            if (fileInterface.Disposed) { throw new ObjectDisposedException("Base FileInterface is disposed."); }

            IOTask task;

            lock (handle)
            {
                handle.Write(buffer, offset, count);
                task = handle.Next();
            }

            await task.WaitAsync();
        }
        protected override void Dispose(bool disposing)
        {
            handle.Dispose();

            base.Dispose(disposing);
        }
    }
}