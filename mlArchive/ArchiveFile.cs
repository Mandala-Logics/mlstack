using DDEncoder;
using DDLib;
using mlStringValidation.Path;
using System;
using System.IO;
using System.Net.Http;

namespace ArcV4
{
    public sealed class ArchiveFile : FileTableEntry
    {
        //PUBLIC PROPERTIES
        public long FileLength
        {
            get
            {
                var bte = Archive[StartBlock];

                long len = bte.UsedBytes;

                while (bte.HasNext)
                {
                    bte = Archive[bte.NextBlock];

                    len += bte.UsedBytes;
                }

                return len;
            }
        }
        public bool StreamOpen => streamCount > 0;
        public override AccessLevel Access => GetAccess();

        //PRIVATE PROPERTIES
        private FileShare share;
        private volatile int streamCount = 0;

        //CONSTRCUTORS
        internal ArchiveFile(ArchiveDir owner, FileSystemID id) : base(id, owner) { }
        public ArchiveFile(EncodedObject encodedObj) : base(encodedObj) { }

        //PUBLIC METHODS
        public ArchiveStream OpenStream(FileAccess fileAccess, FileShare fileShare)
        {
            ThrowIfDeleted();

            if (StreamOpen && fileShare == FileShare.None) throw new FileIsOpenException(ToString());

            if (streamCount > 0)
            {
                switch (fileAccess)
                {
                    case FileAccess.Read:
                        if (share != FileShare.Read && share != FileShare.ReadWrite) throw new FileIsOpenException(ToString());
                        break;
                    case FileAccess.ReadWrite:
                        if (share != FileShare.ReadWrite) throw new FileIsOpenException(ToString());
                        else if (Archive.Packaged) throw new ArchivePackagedException();
                        break;
                    case FileAccess.Write:
                        if (share != FileShare.Write && share != FileShare.ReadWrite) throw new FileIsOpenException(ToString());
                        else if (Archive.Packaged) throw new ArchivePackagedException();
                        break;
                    default:
                        break;
                }
            }

            var ret = new ArchiveStream(Archive, this, fileAccess);

            streamCount++;
            
            if (streamCount == 1) share = fileShare;

            return ret;
        }
        public override void Delete()
        {
            if (streamCount > 0) throw new FileIsOpenException(ToString());

            base.Delete();
        }
        public AccessLevel GetAccess()
        {
            if (Deleted) return AccessLevel.None;

            AccessLevel ret = AccessLevel.FullAccess;

            if (streamCount > 0)
            {
                if (share.HasFlag(FileShare.Read)) ret |= AccessLevel.Read;

                if (!Archive.Packaged)
                {
                    if (share.HasFlag(FileShare.Write)) ret |= AccessLevel.Write;

                    if (share.HasFlag(FileShare.Delete)) ret |= AccessLevel.Delete;
                }                
            }

            return ret;
        }

        //INTERNAL METHODS
        internal void StreamClosed()
        {
            streamCount--;
        }

        //ENCODING
        public override void Encode(ref EncodedObject encodedObj)
        {
            base.Encode(ref encodedObj);
        }
    }
}
