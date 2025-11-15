using DDLib;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using mlFileInterface;
using mlAutoCollection.Sync;
using mlThreadMGMT;
using System.Xml.Schema;

namespace ArcV4
{
    public sealed class ArchiveStream : Stream
    {
        //PUBLIC PROPERTIES
        public override bool CanRead { get; } = true;
        public override bool CanSeek => true;
        public override bool CanWrite { get; } = true;
        public override long Length
        {
            get
            {
                long ret = 0L;

                lock (ranges.SyncRoot)
                {
                    for (int x = 0; x < ranges.Count - 1; x++) { ret += ranges[x].Length; }

                    return ret + Archive[ranges[^1].Key].UsedBytes;
                } 
            }
        }
        public override long Position
        {
            get => pos;
            set
            {
                if (value < 0L) throw new ArgumentOutOfRangeException($"Position ({value}) is less than zero.");
                else if (value > MaxPosition)
                {
                    SetLength(value);
                }

                pos = value;
            }
        }
        public bool EndOfStream => pos == Length;
        public ArchiveV4 Archive { get; }

        //INTERNAL PROPERTIES
        internal int StartBlock { get; private set; }
        internal int WriteSlack
        {
            get
            {
                if (pos == ChainLength) { return 0; }

                long len = pos;                

                for (int x = 0; x < ranges.Count; x++) 
                { 
                    if (len < ranges[x].Length)
                    {
                        return (int)(ranges[x].Length - len);
                    }

                    len -= ranges[x].Length;
                }

                throw new ArgumentOutOfRangeException($"Position ({pos}) is out of range?");
            }
        }
        internal int ReadSlack
        {
            get
            {
                if (pos == MaxPosition) { return 0; }

                long len = pos;
                BlockTableEntry bte;

                for (int x = 0; x < ranges.Count; x++)
                {
                    bte = Archive[ranges[x].Key];

                    if (len < bte.UsedBytes)
                    {
                        return (int)(bte.UsedBytes - len);
                    }

                    len -= bte.UsedBytes;
                }

                throw new ArgumentOutOfRangeException($"Position ({pos}) is out of range?");
            }
        }
        internal long ChainLength
        {
            get
            {
                long ret = 0L;

                foreach (IORange range in ranges) { ret += range.Length; }

                return ret;
            }
        }
        internal long ChainSlack => ChainLength - pos;
        internal long MaxPosition => Length;
        internal long FilePosition
        {
            get
            {
                lock (ranges.SyncRoot)
                {
                    if (pos == 0) { return ranges[0].StartPosition; }
                    else if (pos == ChainLength) throw new InvalidOperationException("No chain slack avalible for writing.");

                    long len = pos;
                
                    for (int x = 0; x < ranges.Count; x++)
                    {
                        if (len < ranges[x].Length)
                        {
                            return ranges[x].EndPosition - (ranges[x].Length - len);
                        }
                        else if (len == ranges[x].Length)
                        {
                            return ranges[x + 1].StartPosition;
                        }
                        else
                        {
                            len -= ranges[x].Length;
                        }
                    }

                }

                throw new ArgumentOutOfRangeException($"Position ({pos}) is out of range?");
            }
        }
        internal BlockTableEntry LastBTE => Archive[ranges[ranges.Count - 1].Key];

        //PRIVATE FIELDS
        private long pos = 0L;
        private readonly SyncedList<IORange> ranges = new SyncedList<IORange>();
        private readonly ArchiveFile file;
        
        //CONSTRUCTORS
        internal ArchiveStream(ArchiveV4 owner, int startBlock)
        {
            Archive = owner ?? throw new ArgumentNullException("owner");

            StartBlock = startBlock;

            BlockTableEntry bte;

            do
            {
                bte = Archive[startBlock];

                ranges.Add(bte.BlockRange);

            } while ((startBlock = bte.NextBlock) > 0);
        }
        internal ArchiveStream(ArchiveV4 owner, ArchiveFile file, FileAccess fileAccess) : this(owner, file.StartBlock)
        {
            this.file = file;

            CanWrite = fileAccess == FileAccess.Write || fileAccess == FileAccess.ReadWrite;
            CanRead = fileAccess == FileAccess.Read || fileAccess == FileAccess.ReadWrite;
        }

        //PUBLIC METHODS
        public override void Flush() => WriteChain(null);
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return 0; }
                 
            long len = 0L;

            using (var handle = DoRead(count, default))
            {
                IOTask task;

                while (!handle.Empty)
                {
                    task = handle.WaitNext();

                    task.Bytes.CopyTo(buffer, offset);

                    offset += (int)task.Length;
                    len += task.Length;
                }
            }

            return (int)len;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            var handle = DoWrite(buffer, offset, count, default);

            //handle.WaitAll();
            //handle.Dispose();
        }
        public IOHandle DoRead(int count, CancellationToken cancellationToken)
        {
            if (!CanRead) throw new IOException("This stream does not support reading.");

            if (count == 0) { return Archive.Interface.GetHandle(); }

            count = (int)Math.Min(Length - pos, count);

            if (count == 0) { return Archive.Interface.GetHandle(); }

            int len;
            var handle = Archive.Interface.GetHandle();

            do
            {
                len = Math.Min(count, ReadSlack);
                count -= len;

                handle.Position = FilePosition;
                handle.Read(len, cancellationToken);

                pos += len;

            } while (count > 0);

            return handle;
        }
        public IOHandle DoWrite(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite) throw new IOException("This stream is read-only.");

            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return Archive.Interface.GetHandle(); }

            IOHandle handle;

            lock (ranges.SyncRoot)
            {
                while (count > ChainSlack) { AddBlock(count - ChainSlack); }

                handle = Archive.Interface.GetHandle();
                int len;

                do
                {
                    len = Math.Min(count, WriteSlack);
                    count -= len;

                    handle.Position = FilePosition;
                    handle.Write(buffer, offset, len, cancellationToken);

                    pos += len;
                    offset += len;

                } while (count > 0);

                if (pos > Length) UpdateUsedBytes(pos);
            }

            Flush();
            //Console.WriteLine($"DoWrite() -> Flush() [{StartBlock}]");

            return handle;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {               
                case SeekOrigin.Current:
                    return Position += offset;
                case SeekOrigin.End:
                    return Position = MaxPosition - offset;
                case SeekOrigin.Begin:
                default:
                    return Position = offset;
            }
        }
        public override void SetLength(long value)
        {
            lock (ranges.SyncRoot)
            {
                if (value < 0) throw new ArgumentOutOfRangeException($"Stream length ({value}) cannot be less than zero.");
                else if (value == Length) { return; }
                else if (value > ChainLength)
                {
                    do
                    {
                        AddBlock(ArchiveConstants.DefualtBlockLength);
                    } while (value > ChainLength);
                }
                else //value < chainlength
                {
                    long len = 0L;
                    int newCount = 0;
                    int x;

                    for (x = 0; x < ranges.Count; x++)
                    {
                        len += ranges[x].Length;

                        if (len >= value)
                        {
                            newCount = x + 1;
                            break;
                        }
                    }

                    if (newCount != ranges.Count)
                    {
                        while (ranges.Count > newCount)
                        {
                            ranges.RemoveAt(ranges.Count - 1);
                        }

                        x = LastBTE.NextBlock;

                        LastBTE.EndChainHere();
                        Archive.DeallocateChainFrom(x);
                    }
                }

                UpdateUsedBytes(value);       

                if (pos > Length) { pos = Length; }         
            }

            Flush();
        }        
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return DoWrite(buffer, offset, count, cancellationToken).WaitAllAsync();
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer is null) throw new ArgumentNullException("buffer is null.");
            else if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            else if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Both count and offset must be greater than zero.");
            else if (count == 0) { return 0; }

            using (var handle = DoRead(count, cancellationToken))
            {
                long len = 0L;
                IOTask task;

                while (!handle.Empty)
                {
                    task = await handle.WaitNextAsync();

                    task.Bytes.CopyTo(buffer, offset);

                    offset += (int)task.Length;
                    len += task.Length;
                }

                return (int)len;
            }
        }
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Flush();

            return Task.CompletedTask;
        }
        public override int ReadByte()
        {
            return DoRead(1, default).WaitNext().Bytes[0];
        }
        public override void WriteByte(byte value)
        {
            var buffer = new byte[1] { value };

            DoWrite(buffer, 0, 1, default).Dispose();
        }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (Length == pos) throw new EndOfStreamException();
            else if (bufferSize < ArchiveConstants.MinBlockLength) { bufferSize = ArchiveConstants.MinBlockLength; }
            else if (bufferSize > ArchiveConstants.MaxBlockLength) { bufferSize = ArchiveConstants.MaxBlockLength; }

            using (var handle = Archive.Interface.GetHandle())
            {
                IOTask task;
                int len;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    len = Math.Min(bufferSize, ReadSlack);

                    handle.Position = FilePosition;
                    handle.Read(len, cancellationToken);
                    pos += len;

                    task = await handle.WaitNextAsync();

                    await destination.WriteAsync(task.Bytes, 0, len, cancellationToken);

                } while (pos < Length);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Flush();

            file?.StreamClosed();
        }

        //PRIVATE METHODS
        private void AddBlock(long blockLength)
        {
            var bte = Archive.GetEmptyBlock(LastBTE.Type, blockLength);
            LastBTE.SetNextBlock(bte.BlockID);

            ranges.Add(bte.BlockRange);
        }
        private void UpdateUsedBytes(long chainLength)
        {
            BlockTableEntry bte;
            long len = 0L;

            for (int x = 0; x < ranges.Count - 1; x++)
            {
                bte = Archive[ranges[x].Key];

                bte.SetUsedBytes(ranges[x].Length);

                len += bte.UsedBytes;
            }

            bte = Archive[ranges[^1].Key];

            bte.SetUsedBytes(chainLength - len);
        }
        private void WriteChain(ThreadController tc)
        {
            foreach (IORange range in ranges)
            {
                Archive.WriteBlock(range.Key);
                if (tc?.Aborting ?? false) return;
            }

            //Console.WriteLine($"WriteChain() Complete -> {StartBlock}");
        }

        //OBJECT OVERRIDES
        public override bool Equals(object obj)
        {
            return obj is ArchiveStream stream &&
                   EqualityComparer<ArchiveV4>.Default.Equals(Archive, stream.Archive) &&
                   StartBlock == stream.StartBlock;
        }
        public override int GetHashCode()
        {
            int hashCode = 2013801085;
            hashCode = hashCode * -1521134295 + EqualityComparer<ArchiveV4>.Default.GetHashCode(Archive);
            hashCode = hashCode * -1521134295 + StartBlock.GetHashCode();
            return hashCode;
        }        
    }
}
