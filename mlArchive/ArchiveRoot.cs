using System;
using System.Collections.Generic;
using DDLib;
using mlStringValidation.Path;

namespace ArcV4
{
    public sealed class ArchiveRoot : ArchiveDir
    {
        //EVENTS
        public event FileSystemChangeEventHandler FileSystemChange;

        //PUBLIC PROPERTIES
        public override ArchiveV4 Archive { get; }
        public override string Name
        {
            get => Archive?.ArchiveName ?? throw new NullReferenceException("Archive is not set.");
            set => throw new CannotModifyRootDirException();
        }
        public override ArchiveDir Root => this;
        public override AccessLevel Access => Archive.Disposed ? AccessLevel.None : AccessLevel.ReadWrite;

        //CONSTRCUTORS
        internal ArchiveRoot(ArchiveV4 owner) : base(1)
        {
            Archive = owner;

            dirCount = Archive.FileHeader.RootDirs;
            fileCount = Archive.FileHeader.RootFiles;
        }

        //PUBLIC METHODS
        public override void Delete() => throw new CannotModifyRootDirException();
        public override void Rename(string newName) => throw new CannotModifyRootDirException();

        //OVERRIDES
        protected override void AfterChanged(PathChangeType reason)
        {
            base.AfterChanged(reason);

            Archive.WriteFileHeader();          
        }
        protected override void OnChildChanged(FileTableEntry fte, PathChangeType reason)
        {
            base.OnChildChanged(fte, reason); 
        }
        public override string ToString() => Name + @":\";

        //INTERNAL METHODS
        internal void OnEntryChanged(FileTableEntry fte, PathChangeType reason)
        {
            FileSystemChange?.Invoke(this, new FileSystemChangeEventArgs(reason, fte));
        }
    }
}
