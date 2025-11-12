using System;
using System.Collections.Generic;
using DDEncoder;
using DDLib;
using mlAutoCollection;
using mlStringValidation.Path;

namespace ArcV4
{
    public enum FTEType : byte { Null = 0, Dir = 1, File = 2, Root = 3 }

    public abstract class FileTableEntry : ICollectable, IEquatable<FileTableEntry>, IEncodable
    {
        public event FileSystemChangeEventHandler EntryChanged;

        //PUBLIC PROPERTIES
        public FileSystemID ID { get; }
        public FTEType Type => ID.Type;
        public virtual string Name
        {
            get => ID.Name;
            set
            {
                ThrowIfDeleted();

                ID.SetName(value);
                AfterChanged(PathChangeType.Renamed);
            }
        }
        public ArchiveDir Owner { get; private set; }
        public int Depth { get; private set; }
        public virtual ArchiveDir Root => Owner.Root;
        public virtual ArchiveV4 Archive => Root.Archive;
        public bool Deleted { get; private set; }
        public bool IsFile => Type == FTEType.File;
        public bool IsDir => Type == FTEType.Dir || Type == FTEType.Root;
        public bool IsRoot => Type == FTEType.Root;
        public ArcPath Path => new ArcPath(this);
        public abstract AccessLevel Access { get; }

        //INTERNAL PROPERTIES
        internal bool Changed => changed && !Deleted;
        internal int StartBlock { get; } = -1;

        //INTERFACE PROPERTIES
        object ICollectable.Key => ID;
        CollectionHandle ICollectable.Handle => collectionHandle;

        //PRIVATE PROPERTIES
        protected readonly CollectionHandle collectionHandle = new CollectionHandle();
        private volatile bool changed = true;

        //CONSTRUCTORS
        internal FileTableEntry(string name, FTEType type, ArchiveDir owner)
        {
            ID = new FileSystemID(collectionHandle, name, type);
            Owner = owner;
            Depth = Owner.Depth + 1;
            StartBlock = Archive.GetEmptyBlock(type == FTEType.Dir ? BlockType.FileTable : BlockType.Binary, ArchiveConstants.DefualtFileTableLength).BlockID;
            Archive.WriteBlock(StartBlock);
        }
        internal FileTableEntry(FileSystemID id, ArchiveDir owner)
        {
            ID = id ?? throw new ArgumentNullException("id");
            ID.Handle = collectionHandle;
            Owner = owner;
            Depth = Owner.Depth + 1;
            StartBlock = Archive.GetEmptyBlock(id.Type == FTEType.Dir ? BlockType.FileTable : BlockType.Binary, ArchiveConstants.DefualtFileTableLength).BlockID;
            Archive.WriteBlock(StartBlock);
        }
        internal FileTableEntry(FileSystemID id)
        {
            ID = id ?? throw new ArgumentNullException("id");
            ID.Handle = collectionHandle;
            Owner = null;
            StartBlock = Archive.GetEmptyBlock(id.Type == FTEType.Dir ? BlockType.FileTable : BlockType.Binary, ArchiveConstants.DefualtFileTableLength).BlockID;
            Archive.WriteBlock(StartBlock);
        }
        internal FileTableEntry(int blockID)
        { 
            ID = new FileSystemID(collectionHandle, "root", FTEType.Root);
            Depth = 0;
            Owner = null;
            StartBlock = blockID;
        }
        internal FileTableEntry(EncodedObject encodedObj)
        {
            try
            {
                ID = encodedObj.Next<FileSystemID>();
                StartBlock = encodedObj.Next<int>();
                Deleted = encodedObj.Next<bool>();
            }
            catch (EncodingException e)
            {
                throw new BadBinaryException("Failed to read file table entry", e);
            }

            ID.Handle = collectionHandle;

            changed = false;
        }

        //PUBLIC METHODS
        public virtual void Delete()
        {
            ThrowIfDeleted();

            Deleted = true;
            Archive.DeallocateChainFrom(StartBlock);

            AfterChanged(PathChangeType.Deleted);
        }
        public virtual void Rename(string newName)
        {
            ThrowIfDeleted();
            ID.SetName(newName);

            AfterChanged(PathChangeType.Renamed);
        }
        public virtual void Encode(ref EncodedObject encodedObj)
        {
            ThrowIfDeleted();

            encodedObj.Append(ID);
            encodedObj.Append(StartBlock);
            encodedObj.Append(Deleted);

            changed = false;
        }

        //PROTECTED METHODS
        protected void ThrowIfDeleted() { if (Deleted) throw new EntryDeletedException(ToString()); }
        protected ArchiveStream GetStream()
        {
            ThrowIfDeleted();
            //Console.WriteLine($"GetStream() [{StartBlock}]");
            return Archive.GetStream(StartBlock);
        }
        internal virtual void SetOwner(ArchiveDir owner)
        {
            ThrowIfDeleted();

            Owner = owner ?? throw new ArgumentNullException("owner");
            Depth = Owner.Depth + 1;
        }
        protected virtual void AfterChanged(PathChangeType reason)
        {
            changed = true;

            Owner?.OnChildChanged(this, reason);
            ((ArchiveRoot)Root).OnEntryChanged(this, reason);
            EntryChanged?.Invoke(this, new FileSystemChangeEventArgs(reason, this));
        }
        protected virtual void OnChildChanged(FileTableEntry fte, PathChangeType reason) { }

        //OBJECT OVERRIDES
        public override string ToString()
        {
            List<string> s = new List<string>(Depth) { Name };

            FileTableEntry fte = Owner;

            while (!fte.IsRoot)
            {               
                s.Add(fte.Name);
                fte = fte.Owner;
            }

            s.Add(fte.Name + ":");

            s.Reverse();

            return string.Join(@"\", s);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as FileTableEntry);
        }
        public bool Equals(FileTableEntry other)
        {
            return other != null &&
                   ID.Equals(other.ID) &&
                   EqualityComparer<ArchiveDir>.Default.Equals(Owner, other.Owner) &&
                   Deleted == other.Deleted;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ID, Owner);
        }
    }
}
