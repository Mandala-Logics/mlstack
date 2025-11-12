using DDEncoder;
using DDLib;
using mlStringValidation.Path;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArcV4
{
    public sealed class ArcPath : PathBase
    {
        //STATIC PROPERTIES
        public static PathStructure PathStructure { get; } = new PathStructure("Archive", typeof(ArcPath), @"\", firstSeperator: @":\");        

        //PUBLIC PROPERTIES
        public FileTableEntry Entry
        {
            get
            {
                if (fte is null) { fte = GetFTE(); }

                return fte;
            }
        }
        public ArchiveDir OwningEntry
        {
            get
            {
                if (!Exists) throw new IOException($"{(IsFile ? "File" : "Dir")} does not exist: {Path}");
                else if (Entry.Type == FTEType.Root) throw new InvalidOperationException("Archive root dirs have no owner.");
                else
                {
                    return Entry.Owner;
                }
            }
        }
        public ArchiveV4 Archive
        {
            get
            {
                if (!Exists) throw new IOException($"{(IsFile ? "File" : "Dir")} does not exist: {Path}");
                else
                {
                    return Entry.Archive;
                }
            }
        }
        public ArchiveDir Root
        {
            get
            {
                if (!Exists) throw new IOException($"{(IsFile ? "File" : "Dir")} does not exist: {Path}");
                else
                {
                    return Entry.Root;
                }
            }
        }
        public override bool Exists => Entry is FileTableEntry && !Entry.Deleted;
        public override AccessLevel Access => (Entry is null || Entry.Deleted) ? AccessLevel.None : Entry.Access;
        public override string HostName => Environment.MachineName;

        //PRIVATE PROPERTIES
        private FileTableEntry fte = null;

        //CONSTRCUTORS
        public ArcPath(string path, DestType mode) : base(PathStructure, CleanPath(path), mode) { }
        public ArcPath(string path) : base(PathStructure, CleanPath(path)) { }
        internal ArcPath(FileTableEntry fte) : base(PathStructure, fte.ToString(), fte.IsFile ? DestType.File : DestType.Dir) { this.fte = fte; }
        private ArcPath(ArcPath path) : base(path) { }
        public ArcPath(EncodedObject encodedObject) : base(PathStructure, encodedObject) { }

        //STATIC CONSTRCUTOR
        static ArcPath() { DDEncoder.DDEncoder.RegisterType(typeof(ArcPath)); }

        //PRIVATE METHODS
        private FileTableEntry GetFTE()
        {
            if (IsAbsolutePath)
            {
                int index = ArchiveManager.Archives.IndexOfKey(base[0]);

                if (index == -1) return null;
                else if (Count == 1) return ArchiveManager.Arcs[index].Root;
                else
                {
                    ArchiveDir dir = ArchiveManager.Arcs[index].Root;

                    for (int x = 1; x < Count - 1; x++)
                    {
                        index = dir.IndexOfKey(new FileSystemID(base[x], FTEType.Dir));

                        if (index == -1) return null;

                        dir = (ArchiveDir)dir[index];
                    }

                    var ret = dir.FirstOrDefault((kvp) => { return StringComparer.OrdinalIgnoreCase.Equals(base[Count - 1], kvp.Value.Name); });

                    return ret.Value;
                }
            }
            else
            {
                return null;
            }
        }

        //PUBLIC METHODS
        public override PathBase GetContainingDir()
        {
            if (Exists && !IsRootPath) return Entry.Owner.Path;
            else { return base.GetContainingDir(); }
        }
        public override IEnumerable<PathBase> Siblings()
        {
            if (!IsAbsolutePath) throw new PathTypeException($"Cannot get siblings for a realitive path: {Path}");
            else if (IsRootPath) throw new PathTypeException($"Cannot get siblings for a root path: {Path}");

            if (!Exists)
            {
                var dir = GetContainingDir();

                if (dir.Exists)
                {
                    return dir.Dir();
                }
                else { throw new PathAccessException(this, $"Cannot get siblings; this path ({Path}) does not exist and the contaning path ({dir.Path}) does not exist."); }
            }
            else
            {
                IEnumerable<FileTableEntry> entries = Entry.Owner.Dir.Where((fte) => !fte.Equals(Entry));

                var ret = new List<ArcPath>(entries.Count());

                foreach (FileTableEntry fte in entries) { ret.Add(fte.Path); }

                return ret;
            }            
        }
        public override PathBase GetRootPath()
        {
            if (Exists) return Entry.Root.Path;
            else { return base.GetRootPath(); }            
        }
        public override PathBase CreateDirectory(string name)
        {
            if (!IsAbsolutePath) throw new PathTypeException($"Cannot create a dir within a realitive path: {Path}");
            else if (!Exists) throw new PathAccessException(this, $"This folder ({Path}) does not exist. Cannot create a dir here.");

            if (Entry is ArchiveDir dir) { return dir.CreateDir(name).Path; }
            else
            {
                throw new PathTypeException($"Cannot create a directory within a file path: {Path}");
            }
        }
        public override IEnumerable<PathBase> Dir()
        {
            if (!IsAbsolutePath) throw new PathTypeException($"Cannot get Dir() for a realitive path: {Path}");
            else if (!Exists) throw new PathAccessException(this, $"This folder ({Path}) does not exist.");

            if (Entry is ArchiveDir dir)
            {
                var ret = new List<ArcPath>(dir.Count);

                foreach (FileTableEntry fte in dir.Dir.GetValues()) { ret.Add(fte.Path); }

                return ret;
            }
            else
            {
                throw new PathTypeException($"Cannot get Dir() for a file path: {Path}");
            }
        }
        public override AccessLevel CheckAccess()
        {
            if (!IsAbsolutePath) throw new PathTypeException("Cannot check access for a non-absolute path.");
            else if (!Exists) return AccessLevel.None;

            if (IsFile) { return ((ArchiveFile)Entry).GetAccess(); }
            else
            {
                if (Archive.Packaged) { return AccessLevel.Read; }
                else { return AccessLevel.FullAccess; }
            }
        }
        public ArchiveFile CreateFile()
        {
            if (Exists) throw new PathAccessException(this, $"A {(IsFile ? "file" : "dir")} already exists at this path: {Path}");

            ArcPath owner = (ArcPath)GetContainingDir();

            if (owner.Entry is null) throw new PathAccessException(owner, $"Cannot create a new file in this directory because it does not exist: {owner}");
            else
            {
                fte = ((ArchiveDir)owner.Entry).CreateFile(base[Count - 1]);

                return (ArchiveFile)fte;
            }
        }
        public override bool CheckExists() => Entry is FileTableEntry;
        public override DestType CheckType()
        {
            if (Exists)
            {
                return Entry.Type == FTEType.Dir || Entry.Type == FTEType.Root ? DestType.Dir : DestType.File;
            }
            else
            {
                return DestType.Unknown;
            }
        }
        public override PathBase Clone() => new ArcPath(this);
        public override void CreateDirectory()
        {
            if (Exists) throw new PathAccessException(this, $"{(IsFile ? "File" : "Dir")} already exists: {Path}");

            ArcPath owner = (ArcPath)GetContainingDir();

            if (owner.Entry is null) throw new PathException($"Cannot create a new directory in this directory because it does not exist: {owner}");
            else
            {
                fte = ((ArchiveDir)owner.Entry).CreateDir(base[Count - 1]);
            }
        }
        public override void Delete(bool recursive = false)
        {
            if (!Exists) throw new PathException($"{(IsFile ? "File" : "Dir")} does not exist: {Path}");

            try { Entry.Delete(); }
            catch (FileIsOpenException)
            {
                throw new PathAccessException(this, "Cannot delete entry while file is open.");
            } 
        }
        public override IEnumerable<PathBase> EnumerateDirs()
        {
            if (!Exists) throw new PathAccessException(this, $"{(IsFile ? "File" : "Dir")} does not exist: {Path}");
            else if (IsFile) throw new PathTypeException($"Cannot get contained dirs for a file path: {Path}");

            ArchiveDir dir = (ArchiveDir)Entry;

            lock (dir.SyncRoot)
            {
                PathBase[] arr = new PathBase[dir.DirCount];
                int x = -1;

                foreach (FileTableEntry fte in dir)
                {
                    if (fte.Type == FTEType.Dir) { arr[++x] = fte.Path; }
                }

                return arr;
            }      
        }
        public override IEnumerable<PathBase> EnumerateFiles()
        {
            if (!Exists) throw new PathAccessException(this, $"{(IsFile ? "File" : "Dir")} does not exist: {Path}");
            else if (IsFile) throw new PathTypeException($"Cannot get contained files for a file path: {Path}");

            ArchiveDir dir = (ArchiveDir)Entry;

            lock (dir.SyncRoot)
            {
                PathBase[] arr = new PathBase[dir.FileCount];
                int x = -1;

                foreach (FileTableEntry fte in dir)
                {
                    if (fte.Type == FTEType.File) { arr[++x] = fte.Path; }
                }

                return arr;
            }
        }
        public override Stream OpenStream(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (!IsFile) throw new PathTypeException($"This path does not point to a file: {this}");
            else if (!IsAbsolutePath) throw new PathTypeException($"This is not an absolute path: {this}");

            ArchiveStream ret;

            switch (fileMode)
            {
                case FileMode.Append:

                    if (!Exists) throw new PathAccessException(this, $"File does not exist: {this}");
                    ret = ((ArchiveFile)Entry).OpenStream(fileAccess, fileShare);
                    ret.Seek(0, SeekOrigin.End);
                    break;

                case FileMode.Create:

                    if (Exists)
                    {
                        ret = ((ArchiveFile)Entry).OpenStream(fileAccess, fileShare);
                        ret.SetLength(0);
                    }
                    else
                    {
                        ret = CreateFile().OpenStream(fileAccess, fileShare);                      
                    }
                    break;

                case FileMode.CreateNew:

                    if (Exists) throw new PathAccessException(this, $"File already exists and Filemode is CreateNew: {this}");

                    ret = CreateFile().OpenStream(fileAccess, fileShare);

                    break;

                case FileMode.Open:

                    if (!Exists) throw new PathAccessException(this, $"File does not exist: {this}");
                    ret = ((ArchiveFile)Entry).OpenStream(fileAccess, fileShare);
                    break;

                case FileMode.OpenOrCreate:

                    if (Exists)
                    {
                        ret = ((ArchiveFile)Entry).OpenStream(fileAccess, fileShare);
                    }
                    else
                    {
                        ret = CreateFile().OpenStream(fileAccess, fileShare);
                    }

                    break;

                case FileMode.Truncate:

                    if (!Exists) throw new PathAccessException(this, $"File does not exist: {this}");
                    ret = ((ArchiveFile)Entry).OpenStream(fileAccess, fileShare);
                    ret.SetLength(0);
                    break;

                default:
                    throw new ArgumentException("Filemode is not defined.");
            }

            return ret;
        }
        public override void StartWatchingPath()
        {
            if (!IsAbsolutePath) throw new PathTypeException($"Cannot watch a path ({Path}) which is not an abolsute path.");
            else if (!Exists) throw new PathAccessException(this, $"This path ({Path}) does not exist.");
            else if (Entry.Deleted) throw new EntryDeletedException("This entry is deleted: " + Path);

            Entry.EntryChanged += WatchChange;

            if (Entry.IsDir)
            {
                ((ArchiveDir)Entry).ChildChanged += WatchChange;
            }

            WatchingPath = true;
        }       
        public override void StopWatchingPath()
        {
            if (!WatchingPath) return;

            Entry.EntryChanged -= WatchChange;

            if (Entry.IsDir)
            {                
                ((ArchiveDir)Entry).ChildChanged -= WatchChange;
            }

            WatchingPath = false;
        }

        //EVENT HANDLERS
        private void WatchChange(object sender, FileSystemChangeEventArgs args)
        {
            if (args.Type == PathChangeType.Deleted) { StopWatchingPath(); }

            ArcPath path;

            if (args.Type.HasFlag(PathChangeType.ChildMask))
            {
                path = args.Entry.Path;
            }
            else
            {
                path = this;
            }

            OnPathChanged(new PathChangedEventArgs(args.Type, path));
        }

        public override long FileLength()
        {
            if (IsFile) { return ((ArchiveFile)Entry).FileLength; }
            else { throw new PathException("Cannot get file length for a dir path: " + ToString()); }
        }

        public override PathBase GetWorkingDirectory() => throw new NotSupportedException();
    }
}
