using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DDEncoder;
using mlAutoCollection.Sync;
using mlFileInterface;
using mlStringValidation.Path;

namespace mlEncodedDB
{
    public sealed partial class BlockList : IDisposable, IReadOnlyList<IEncodable>, ICollection<IEncodable>
    {
        internal static int BlockTableLength = 64;

        public IEncodable this[int index] => Get(index);
        public int Count => counter.GetCount();
        public int EnumsOpen => enumsOpen;
        public object SyncRoot = new object();
        bool ICollection<IEncodable>.IsReadOnly => false;

        private readonly FileInterface file;
        internal readonly SyncedList<BlockTableEntry> btes;
        internal readonly BlockListCounter counter;
        internal readonly BlockListCache cache;
        private volatile int enumsOpen = 0;

        static BlockList()
        {
            DDEncoder.DDEncoder.RegisterTypes(typeof(BlockTableEntry));
        }

        public BlockList(PathBase path, FileMode fileMode)
        {
            lock (SyncRoot)
            {
                file = new FileInterface(path, fileMode);
    
                var handle = file.GetHandle();
    
                if (file.Length == 0)
                {
                    var a = new BlockTableEntry(sizeof(int) + 1, BlockTableEntry.EncodedSize * BlockTableLength, -1, false);
    
                    var ev = new EncodedValue(1);
    
                    handle.Encode(1);
                    handle.Encode(a);
    
                    btes = new SyncedList<BlockTableEntry> { a };
                }
                else
                {
                    var task = handle.Decode();
    
                    try { task.Wait(); }
                    catch (EncodingException) { throw new ArgumentException($"File ({path.Path}) does not appear to be valid block list."); }
    
                    if (!(task.DecodedObject.Value is int n))
                    {
                        throw new ArgumentException($"File ({path.Path}) does not appear to be valid block list.");
                    }
    
                    btes = new SyncedList<BlockTableEntry>(n);
    
                    task = handle.Decode();
    
                    try { task.Wait(); }
                    catch (EncodingException) { throw new ArgumentException($"File ({path.Path}) does not appear to be valid block list."); }
    
                    if (!(task.DecodedObject.Value is BlockTableEntry bte))
                    {
                        throw new ArgumentException($"File ({path.Path}) does not appear to be valid block list.");
                    }
    
                    ReadBlockTables(n, bte);
                }
    
                counter = new BlockListCounter(this);
                cache = new BlockListCache(1024);
            }
        }

        public void Add(IEncodable obj)
        {
            lock (SyncRoot)
            {
                if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }
            
                DoAdd(obj);
            }
        }

        internal int DoAdd(IEncodable obj)
        {
            var ms = new MemoryStream();
    
            DDEncoder.DDEncoder.EncodeObject(ms, obj);

            var len = (int)ms.Length;

            var b = ms.GetBuffer();

            var handle = file.GetHandle();
            int n;
            int written = 0;
            int first = -1;

            while (written < len)
            {
                var bte = GetEmptyBlock(len - written);
                btes[bte].Empty = false;
                WriteBTE(bte);

                n = Math.Min(btes[bte].Length, len);

                handle.Seek(btes[bte].StartPos, SeekOrigin.Begin);
                handle.Write(b, written, n);

                handle.WaitAll();

                written += n;

                if (first < 0) { first = bte; }
            }

            cache.TrySet(first, obj);
            counter.Incrament(1);

            return first;
        }

        /// <summary>
        /// I reccomend you take out a lock on SyncRoot before calling this as it will prevent any other enums from opening while you access the list.
        /// </summary>
        /// <returns>True if the number of enums open drops to zero within the timespan.</returns>
        public bool WaitForEnums(TimeSpan timeout)
        {
            return SpinWait.SpinUntil(() => enumsOpen == 0, timeout);
        }

        /// <summary>
        /// I reccomend you take out a lock on SyncRoot before calling this as it will prevent any other enums from opening while you access the list.
        /// </summary>
        public void WaitForEnums() => WaitForEnums(TimeSpan.MaxValue);

        internal int FindBTEIndex(int objIndex)
        {
            lock (SyncRoot)
            {
                if (objIndex < 0) { throw new IndexOutOfRangeException(); }
                else if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }
    
                var skip = new List<int>() { 0 };
    
                int n = -1;
    
                for (int x = 0; x < btes.Count; x++)
                {
                    if (skip.Contains(x))
                    {
                        if (btes[x].NextBlock > 0) { skip.Add(btes[x].NextBlock); }
    
                        continue;
                    }
                    else if (!btes[x].Empty)
                    {
                        n++;
    
                        if (n == objIndex)
                        {
                            return x;
                        }
                    }
    
                    if (btes[x].NextBlock > 0)
                    {
                        skip.Add(btes[x].NextBlock);
                    }
                }
    
                throw new IndexOutOfRangeException();
            }
        }

        public bool Remove(IEncodable obj)
        {
            lock (SyncRoot)
            {
                if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }

                int n = 0;
                bool found = false;

                foreach (IEncodable ie in this)
                {
                    if (ie.Equals(obj))
                    {
                        found = true;
                        break;
                    }

                    n++;
                }

                if (!found) { return false; }

                Remove(n);

                return true;
            }
        }

        public bool Contains(IEncodable item)
        {
            if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }

            lock (SyncRoot)
            {
                bool found = false;

                foreach (IEncodable ie in this)
                {
                    if (ie.Equals(item))
                    {
                        found = true;
                        break;
                    }
                }

                return found;
            }
        }

        public void CopyTo(IEncodable[] array, int arrayIndex)
        {
            if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
            
            lock (SyncRoot)
            {
                if (array.Length < arrayIndex + Count) { throw new InvalidOperationException("Not enough elements in array."); }

                foreach (IEncodable ie in this)
                {
                    array[arrayIndex] = ie;
                    arrayIndex++;
                }
            }
        }

        public IEncodable Get(int objIndex)
        {
            if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }

            var bteIndex = FindBTEIndex(objIndex);

            lock (SyncRoot)
            {
                if (cache.TryGet(bteIndex, out IEncodable? x))
                {
                    return x;
                }
                else
                {
                    var ret = ReadObject(bteIndex);
    
                    cache.TrySet(bteIndex, ret);
    
                    return ret;
                }
            }
        }

        public List<IEncodable> GetAll()
        {
            if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }

            lock (SyncRoot)
            {
                var skip = new List<int>() { 0 };
                var ret = new List<IEncodable>();
    
                for (int x = 0; x < btes.Count; x++)
                {
                    if (skip.Contains(x))
                    {
                        if (btes[x].NextBlock > 0) { skip.Add(btes[x].NextBlock); }
                    }
                    else if (!btes[x].Empty)
                    {
                        if (btes[x].NextBlock > 0) { skip.Add(btes[x].NextBlock); }
    
                        ret.Add(ReadObject(x));
                    }
                }

                return ret;
            }
        }
        public void Clear()
        {
            lock (SyncRoot)
            {
                if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }

                var skip = new List<int>() { 0 };
    
                for (int x = 0; x < btes.Count; x++)
                {
                    if (skip.Contains(x))
                    {
                        if (btes[x].NextBlock > 0) { skip.Add(btes[x].NextBlock); }
                    }
                    else if (!btes[x].Empty)
                    {
                        btes[x].Empty = true;
    
                        WriteBTE(x);
                    }
                }
    
                counter.Clear();
                cache.Clear();
            }
        }

        public void Remove(int objIndex)
        {
            lock (SyncRoot)
            {
                if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }

                var bteIndex = FindBTEIndex(objIndex);
    
                var chain = GetChain(bteIndex);
    
                foreach (var x in chain)
                {
                    btes[x].NextBlock = -1;
                    btes[x].Empty = true;
                    WriteBTE(x);
                }
    
                counter.Decrament(1);
                cache.Remove(bteIndex);
            }
        }

        public int IndexOf(IEncodable obj)
        {
            if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }

            lock (SyncRoot)
            {
                int n = 0;
                bool found = false;

                foreach (IEncodable ie in this)
                {
                    if (ie.Equals(obj))
                    {
                        found = true;
                        break;
                    }

                    n++;
                }

                if (!found) { return n; }

                return -1;
            }
        }

        public void Set(int objIndex, IEncodable obj)
        {
            lock (SyncRoot)
            {
                if (file.Disposed) { throw new ObjectDisposedException("BlockList"); }
                else if (enumsOpen > 0) { throw new InvalidOperationException("BlockList cannot be modified while it is being enumerated."); }

                var bteIndex = FindBTEIndex(objIndex);
    
                using var ms = new MemoryStream();
    
                DDEncoder.DDEncoder.EncodeObject(ms, obj);
    
                var b = ms.GetBuffer();
    
                var handle = file.GetHandle();
    
                ResizeChain(bteIndex, b.Length);
    
                int written = 0;
                int n;
    
                while (written < b.Length)
                {
                    n = Math.Min(btes[bteIndex].Length, b.Length - written);
    
                    handle.Seek(btes[bteIndex].StartPos, SeekOrigin.Begin);
    
                    handle.Write(b, written, n);
    
                    written += n;
    
                    bteIndex = btes[bteIndex].NextBlock;
                }
    
                cache.TrySet(bteIndex, obj);
            }
        }

        private List<int> GetChain(int bteIndex)
        {
            var ret = new List<int>() { bteIndex };

            while (btes[bteIndex].NextBlock > 0)
            {
                ret.Add(btes[bteIndex].NextBlock);

                bteIndex = btes[bteIndex].NextBlock;
            }

            return ret;
        }

        private void ResizeChain(int bteIndex, int length)
        {
            var chain = GetChain(bteIndex);
            int n = -1;
            int l = 0;
            int x;

            for (x = 0; x < chain.Count; x++)
            {
                n++;

                l += btes[chain[x]].Length;

                if (l >= length) { break; }
            }

            if (l < length) //chain needs to be exapanded
            {
                var newBlock = GetEmptyBlock(length - l);

                btes[newBlock].Empty = false;
                WriteBTE(newBlock);

                btes[chain[^1]].NextBlock = newBlock;
                WriteBTE(chain[^1]);
            }
            else if (n < chain.Count - 1) // need to shrink chain
            {
                btes[chain[n]].NextBlock = -1;
                WriteBTE(chain[n]);

                for (x = n + 1; x < chain.Count; x++)
                {
                    btes[chain[x]].NextBlock = -1;
                    btes[chain[x]].Empty = true;
                    WriteBTE(chain[x]);
                }
            }

        }
        
        internal IEncodable ReadObject(int bteIndex)
        {
            var curr = btes[bteIndex];

            int len = curr.Length;

            while (curr.NextBlock > 0)
            {
                curr = btes[curr.NextBlock];

                len += curr.Length;
            }

            var handle = file.GetHandle();

            curr = btes[bteIndex];

            int n = Math.Min(len, curr.Length);
            int read = n;

            handle.Seek(curr.StartPos, SeekOrigin.Begin);
            handle.Read(n);

            while (curr.NextBlock > 0)
            {
                curr = btes[curr.NextBlock];

                n = Math.Min(len - read, curr.Length);

                handle.Seek(curr.StartPos, SeekOrigin.Begin);
                handle.Read(n);

                read += n;
            }

            if (read < n) { throw new Exception("Not able to read full object."); }

            var b = new byte[len];
            n = 0;

            while (n < len)
            {
                var task = handle.Next();

                task.Wait();

                Buffer.BlockCopy(task.Bytes, 0, b, n, task.Bytes.Length);

                n += task.Bytes.Length;
            }

            var ms = new MemoryStream(b);

            DDEncoder.DDEncoder.DecodeObject(ms, out IEncodable obj);

            return obj;
        }

        private void ReadBlockTables(int blockCount, BlockTableEntry first)
        {
            var curr = first;
            int x = 0;
            int n;

            var handle = file.GetHandle();

            do
            {
                handle.Seek(curr.StartPos, SeekOrigin.Begin);

                n = Math.Min(BlockTableLength, blockCount - x);

                for (int c = 1; c <= n; c++)
                {
                    handle.Decode();
                }

                x += n;

                while (handle.Count > 0)
                {
                    var bte = (BlockTableEntry)handle.WaitNext().DecodedObject.Value;

                    btes.Add(bte);
                }

                if (curr.NextBlock > 0)
                {
                    curr = btes[curr.NextBlock];
                }
                else if (curr.NextBlock == -1 && x < blockCount)
                {
                    throw new Exception("Cannot read all blocks?");
                }

            } while (x < blockCount);
        }
        
        private void WriteBlockCount()
        {
            file.Encode(SeekOrigin.Begin, 0L, btes.Count);
        }

        private int GetEmptyBlock(int length)
        {
            for (int x = 0; x < btes.Count; x++)
            {
                if (btes[x].Empty) { return x; }
            }

            return CreateBlock(length);
        }

        private int CreateBlock(int length)
        {
            if (btes.Count % BlockTableLength == BlockTableLength - 4)
            {
                ExpandBlockTable();
            }

            long fileLen = btes[^1].EndPos;

            var bte = new BlockTableEntry(fileLen, Math.Max(length, 128), -1, true);

            btes.Add(bte);

            int n = btes.Count - 1;

            var pos = FindBTEPosition(n);

            var handle = file.GetHandle();

            handle.Seek(pos, SeekOrigin.Begin);

            handle.Encode(btes[n]);

            WriteBlockCount();

            return n;
        }

        private long FindBTEPosition(int bteIndex)
        {
            var tableID = bteIndex / BlockTableLength;

            BlockTableEntry curr = btes[0];

            int x = 0;

            while (curr.NextBlock >= 0 && x < tableID)
            {
                curr = btes[curr.NextBlock];
                x++;
            }

            if (x < tableID) { return -1L; } // x is beyond the end of the chain

            var r = bteIndex % BlockTableLength;

            return curr.StartPos + r * BlockTableEntry.EncodedSize;
        }

        private void WriteBTE(int bteIndex)
        {
            var pos = FindBTEPosition(bteIndex);

            if (pos == -1L) { throw new Exception("Unable to find BTE position."); }
            else
            {
                file.Encode(SeekOrigin.Begin, pos, btes[bteIndex]);
            }
        }

        private void ExpandBlockTable()
        {
            BlockTableEntry curr = btes[0];

            int x = 0;

            while (curr.NextBlock >= 0)
            {
                curr = btes[x = curr.NextBlock];
            }

            var eof = btes[^1].EndPos;

            int n = btes.Count;

            var newBTE = new BlockTableEntry(eof, BlockTableEntry.EncodedSize * BlockTableLength, -1, false);

            var newCurr = new BlockTableEntry(curr.StartPos, curr.Length, n, false);

            btes[x] = newCurr;
            btes.Add(newBTE);

            WriteBTE(n);
            WriteBTE(x);

            WriteBlockCount();
        }

        public IEnumerator<IEncodable> GetEnumerator() { return new BlockListEnumerator(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new BlockListEnumerator(this); }

        public void Dispose()
        {
            file.Dispose();
        }
    }
}
