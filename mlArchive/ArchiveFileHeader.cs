using System;
using System.Text;
using DDEncoder;
using mlStringValidation;
using mlAutoCollection;

namespace ArcV4
{
    internal sealed class ArchiveFileHeader : IEncodable
    {
        //PUBLIC STATIC PROPERTIES
        public static readonly int MaxArchiveNameLength = 128;
        public static readonly int SpareBytesLength = 64;
        public static readonly StringTemplate ArchiveNameTemplate = new StringTemplate(StringValidationFlags.FileName, maxWords: 1, maxLength: MaxArchiveNameLength, minLength: 2);
        
        //PUBLIC PROPERTIES
        public string ArchiveName { get; private set; }
        public int Version { get; }
        public int BlockCount
        {
            get => blockCount;
            set => blockCount = value;
        }
        public bool Packaged { get; set; } 
        public int RootFiles { get; set; }
        public int RootDirs { get; set; }

        //PRIVATE PROPERTIES
        private CollectionHandle handle;
        private volatile int blockCount;

        //CONSTRUCTORS
        internal ArchiveFileHeader(CollectionHandle handle, string archiveName)
        {
            ArchiveName = archiveName;
            this.handle = handle;
            Version = ArchiveConstants.ArchiveVerion; 
        }
        internal ArchiveFileHeader(string archiveName)
        {
            ArchiveName = archiveName;
            Version = ArchiveConstants.ArchiveVerion; 
        }

        //PUBLIC METHODS
        public void SetName(string name)
        {
            try
            {
                name = ArchiveNameTemplate.Validate(name);
                if (name.GetByteLength() > MaxArchiveNameLength) throw new StringValidationException("Name string byte length too long.");
            }
            catch (StringValidationException e) { throw new NameNotAllowedException(name, e); }

            if (handle is CollectionHandle && handle.IsKeyAllowed(name))
            {
                ArchiveName = name;
                handle.OnKeyChanged();
            }
            else
            {
                throw new NameAlreadyTakenException(name);
            }
        }
        public void SetHandle(CollectionHandle handle)
        {
            if (this.handle is null) { this.handle = handle; }
            else throw new InvalidOperationException("Handle is already set.");
        }

        //ENCODING
        public ArchiveFileHeader(EncodedObject eo)
        {
            Version = eo.Next<int>();

            if (Version != 4) throw new BadBinaryException("Invalid archive version.");

            ArchiveName = Encoding.UTF8.GetString(eo.Next<byte[]>()).Trim('\0');

            if (!ArchiveNameTemplate.TryValidate(ArchiveName, out _)) throw new BadBinaryException("Archive name invalid on decoding.");

            blockCount = eo.Next<int>();

            if (BlockCount < 2) throw new BadBinaryException("BlockCount cannot be less than two.");

            Packaged = eo.Next<bool>();            
            
            RootDirs = eo.Next<int>();

            if (RootDirs < 0) throw new BadBinaryException("Root dir cound less than zero.");

            RootFiles = eo.Next<int>();

            if (RootFiles < 0) throw new BadBinaryException("Root dir cound less than zero.");

            _ = eo.Next<byte[]>();

            handle = null;
        }
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(Version);
            encodedObj.Append(ArchiveName.GetFixedBytes(MaxArchiveNameLength));
            encodedObj.Append(blockCount);
            encodedObj.Append(Packaged);
            encodedObj.Append(RootDirs);
            encodedObj.Append(RootFiles);
            encodedObj.Append(new byte[SpareBytesLength]);
        }
    }
}
