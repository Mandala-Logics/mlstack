using DDEncoder;
using DDLib;
using mlAutoCollection;
using mlStringValidation.Path;
using mlThreadMGMT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArcV4
{
    public class ArchiveDir : FileTableEntry, IReadOnlyTypedAutoCollection<FileSystemID, FileTableEntry>
    {
        //EVENTS
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeAdd { add => dir.BeforeAdd += value; remove => dir.BeforeAdd -= value; }
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeRemove { add => dir.BeforeRemove += value; remove => dir.BeforeRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterAdd { add => dir.AfterAdd += value; remove => dir.AfterAdd -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterRemove { add => dir.AfterRemove += value; remove => dir.AfterRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.KeyChanged { add => dir.KeyChanged += value; remove => dir.KeyChanged -= value; }
        event EventHandler IReadOnlyAutoCollection.Cleared { add => dir.Cleared += value; remove => dir.Cleared -= value; }
        public event FileSystemChangeEventHandler ChildChanged;

        //PUBLIC PROPERTIES        
        public int Count => fileCount + dirCount;
        public int FileCount => fileCount;
        public int DirCount => dirCount;
        public FileTableEntry this[int index] => Dir[index];
        public FileTableEntry this[FileSystemID key] => Dir[key];
        public AutoCollection<FileTableEntry> Dir
        {
            get
            {
                ThrowIfDeleted();

                lock (SyncRoot)
                {
                    if (!dirRead)
                    {
                        dir.AddRange(ReadDir(fileCount + dirCount));
                        dirRead = true;
                    }

                    return dir;
                }
            }
        }
        public bool IsSynchronized => dir.IsSynchronized;
        public object SyncRoot => dir.SyncRoot;
        public override AccessLevel Access
        {
            get
            {
                if (Deleted) return AccessLevel.None;
                else return AreAnyFilesOpen() ? AccessLevel.FullAccess ^ AccessLevel.Delete : AccessLevel.FullAccess;
            }           
        }

        //INTERFACE PROPERTIES
        IEqualityComparer IReadOnlyAutoCollection.KeyComparaer => ((IReadOnlyAutoCollection)dir).KeyComparaer;

        //PRIVATE PROPERTIES
        private readonly AutoCollection<FileTableEntry> dir = new AutoCollection<FileTableEntry>(0, true);
        private volatile bool dirRead = false;
        protected volatile int fileCount;
        protected volatile int dirCount;
        private ThreadTask flushThread;

        //PROTECTED PROPERTIES
        protected bool ShouldWriteDir => dirRead && AreAnyChanged() && !Deleted;
        public IEnumerable<FileSystemID> Keys => dir.GetKeys().Cast<FileSystemID>();
        public IEnumerable<FileTableEntry> Values => dir.GetValues();

        //CONSTRCUTORS
        protected internal ArchiveDir(int blockID) : base(blockID) { }
        internal ArchiveDir(FileSystemID id, ArchiveDir owner) : base(id, owner) { }
        public ArchiveDir(EncodedObject encodedObj) : base(encodedObj)
        {
            try
            {
                fileCount = encodedObj.Next<int>();
                dirCount = encodedObj.Next<int>();
            }
            catch (EncodingException e)
            {
                throw new BadBinaryException("Failed to read archive dir/root.", e);
            }            
        }

        //PUBLIC METHODS
        public ArchiveFile CreateFile(string name)
        {
            ArchiveFile ret;

            lock (SyncRoot)
            {
                var key = new FileSystemID(name, FTEType.File);

                if (Dir.ContainsKey(key)) throw new NameAlreadyTakenException(name);                

                Dir.Add(ret = new ArchiveFile(this, key));

                fileCount++;
            }

            AfterChanged(PathChangeType.DirectoryChanged);

            return ret;
        }
        public ArchiveDir CreateDir(string name)
        {
            ArchiveDir ret;

            lock (SyncRoot)
            {
                var key = new FileSystemID(name, FTEType.Dir);

                if (Dir.ContainsKey(key)) throw new NameAlreadyTakenException(name);                

                Dir.Add(ret = new ArchiveDir(key, this));

                dirCount++;                
            }

            AfterChanged(PathChangeType.DirectoryChanged);

            return ret;         
        }
        public IEnumerable<ArchiveFile> EnumerateFiles() => Dir.TakeWhile((fte) => fte.Type == FTEType.File).Cast<ArchiveFile>();
        public IEnumerable<ArchiveDir> EnumerateDirs() => Dir.TakeWhile((fte) => fte.Type == FTEType.Dir).Cast<ArchiveDir>();
        public override void Delete()
        {
            if (AreAnyFilesOpen()) throw new FileIsOpenException();

            foreach (FileTableEntry fte in dir) { fte.Delete(); }

            dir.Clear();
            dirRead = false;
            fileCount = dirCount = 0;

            base.Delete();            
        }
        public override void Encode(ref EncodedObject encodedObj)
        { 
            base.Encode(ref encodedObj);

            encodedObj.Append(FileCount);
            encodedObj.Append(DirCount);
        }
        public void ForEachDir(Action<ArchiveDir> action)
        {
            ArchiveDir d;

            foreach (FileTableEntry fte in Dir)
            {
                if (fte.Type == FTEType.Dir)
                {
                    d = (ArchiveDir)fte;

                    try { action.Invoke(d); }
                    catch (TargetInvocationException e) { throw e.InnerException; }
                }
            }
        }
        public void ForEachFile(Action<ArchiveFile> action)
        {
            ArchiveFile f;

            foreach (FileTableEntry fte in Dir)
            {
                if (fte.Type == FTEType.File)
                {
                    f = (ArchiveFile)fte;

                    try { action.Invoke(f); }
                    catch (TargetInvocationException e) { throw e.InnerException; }
                }
            }
        }
        public bool AreAnyFilesOpen() => EnumerateFiles().Any((af) => af.StreamOpen);

        //OVERRIDES
        protected override void OnChildChanged(FileTableEntry fte, PathChangeType reason)
        {
            if (reason == PathChangeType.Deleted)
            {
                if (fte.IsFile) fileCount--;
                else dirCount--;

                Dir.Remove(fte);
            }

            Flush();

            if (reason != PathChangeType.None)
            {
                ChildChanged?.Invoke(this, new FileSystemChangeEventArgs(reason | PathChangeType.ChildChanged, fte));                
            }            

            base.OnChildChanged(fte, reason);
        }
        protected override void AfterChanged(PathChangeType reason)
        {
            base.AfterChanged(reason);

            Flush();
        }

        //PRIVATE METHODS
        private void Flush()
        {
            if (flushThread?.ThreadRunning ?? false) { flushThread.AwaitAbort(); }

            flushThread = Archive.Threads.StartTask(DoFlush, ThreadTaskPriority.MustBeCompleted);            
        }
        private IEnumerable<FileTableEntry> ReadDir(int count)
        { 
            if (count == 0)
            {
                return Enumerable.Empty<FileTableEntry>();
            }

            FileTableEntry[] entries = new FileTableEntry[count];

            using (DDEncoder.DDEncoder enc = new DDEncoder.DDEncoder(GetStream(), true))
            {
                for (int x = 0; x < count; x++)
                {
                    enc.Read(out entries[x]);
                    entries[x].SetOwner(this);
                }
            }

            return entries;
        }
        private bool AreAnyChanged() => dir.Any((fte) => fte.Changed);
        internal void DoFlush(ThreadController tc)
        {
            if ((dirCount == 0 && fileCount == 0) || !ShouldWriteDir) { return; }

            using (DDEncoder.DDEncoder enc = new DDEncoder.DDEncoder(GetStream(), true))
            {
                foreach (FileTableEntry fte in Dir)
                {
                    tc?.ThrowIfAborted();

                    if (fte.Deleted) continue;

                    if (fte.Type == FTEType.File)
                    {
                        enc.Write((ArchiveFile)fte);
                    }
                }

                ArchiveDir dir;

                foreach (FileTableEntry fte in Dir)
                {
                    tc?.ThrowIfAborted();

                    if (fte.Deleted) continue;

                    if (fte.Type == FTEType.Dir)
                    {
                        dir = (ArchiveDir)fte;

                        if (dir.ShouldWriteDir) { dir.Flush(); }

                        enc.Write(dir);
                    }
                }
            }
        }

        //INTERFACE METHODS
        public int IndexOf(FileTableEntry value) => Dir.IndexOf(value);
        public bool Contains(FileTableEntry value) => Dir.Contains(value);
        public FileTableEntry ElementAtKey(FileSystemID key) => Dir.ElementAtKey(key);
        public FileTableEntry ElementAt(int index) => Dir.ElementAt(index);
        public int IndexOfKey(FileSystemID key) => Dir.IndexOfKey(key);
        public bool ContainsKey(FileSystemID key) => Dir.ContainsKey(key);
        public IEnumerator<FileTableEntry> GetEnumerator() => ((IEnumerable<FileTableEntry>)Dir).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dir).GetEnumerator();
        public FileTableEntry ElementAtKey(object key) => Dir[key];
        int IReadOnlyAutoCollection.IndexOfKey(object key) => Dir.IndexOfKey(key);
        IEnumerable<object> IReadOnlyAutoCollection.GetKeys() => Dir.GetKeys();
        bool IReadOnlyAutoCollection.ContainsKey(object key) => Dir.ContainsKey(key);
        ICollectable IReadOnlyAutoCollection.GetValue(object key) => Dir[key];
        object IReadOnlyAutoCollection.GetValue(int index) => Dir[index];
        object IReadOnlyAutoCollection.GetKey(int index) => ((ICollectable)Dir[index]).Key;
        int IReadOnlyAutoCollection.IndexOf(ICollectable value)
        {
            if (value is FileTableEntry val) { return Dir.IndexOf(val); }
            else { return -1; }
        }
        void ICollection.CopyTo(Array array, int index) => ((ICollection)Dir).CopyTo(array, index);
        bool IReadOnlyDictionary<FileSystemID, FileTableEntry>.TryGetValue(FileSystemID key, out FileTableEntry value)
        {
            try
            {
                value = dir[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default;
                return false;
            }
        }
        IEnumerator<KeyValuePair<FileSystemID, FileTableEntry>> IEnumerable<KeyValuePair<FileSystemID, FileTableEntry>>.GetEnumerator()
            => Dir.AsTypedReadOnly<FileSystemID>().GetEnumerator();
    }
}
