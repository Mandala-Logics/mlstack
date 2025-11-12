using System;
using DDEncoder;
using DDLib;
using mlAutoCollection;
using mlStringValidation;

namespace ArcV4
{
    public sealed class FileSystemID : IEncodable, IEquatable<FileSystemID>
    {
        //PUBLIC STATIC PROPERTIES
        public static readonly StringTemplate NameTemplate = new StringTemplate(StringValidationFlags.FileName, maxLength: ArchiveConstants.MaxFileNameLength);

        //PUBLIC PROPERTIES
        public string Name { get; private set; }
        public FTEType Type { get; }

        //INTERNAL PROPERTIES
        internal CollectionHandle Handle { get; set; }

        //CONSTRUCTORS
        internal FileSystemID(CollectionHandle handle, string name, FTEType type)
        {
            Name = NameTemplate.Validate(name);

            Type = type;
            Handle = handle;
        }
        public FileSystemID(string name, FTEType type)
        {
            if (type != FTEType.Dir && type != FTEType.File) throw new ArgumentException("FileSystemID may only be either dir or file.");

            Name = NameTemplate.Validate(name);
            Handle = null;
            Type = type;
        }

        //PUBLIC METHODS
        internal void SetName(string name)
        {
            var prev = Name;

            try
            {
                Name = NameTemplate.Validate(name);
                if (Name.GetByteLength() > ArchiveConstants.MaxFileNameLength) throw new StringValidationException("Name string byte length too long.");
            }
            catch (StringValidationException e)
            {
                Name = prev;
                throw new NameNotAllowedException(name, e);
            }

            if (Handle.IsKeyAllowed(this))
            {
                Handle.OnKeyChanged();
            }
            else
            {
                Name = prev;
                throw new NameAlreadyTakenException(name);
            }
        }

        //ENCODING
        public FileSystemID(EncodedObject encodedObj)
        {
            Type = encodedObj.Next<FTEType>();
            Name = encodedObj.Next<string>().Trim('\0');

            Handle = null;
        }
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(Type);
            encodedObj.Append(Name);
        }

        //OBJECT OVERRIDES
        public override string ToString() => $"[{Type}] {Name}";
        public override bool Equals(object obj) => obj is FileSystemID iD && Equals(iD);
        public bool Equals(FileSystemID other)
        {
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Type == other.Type;
        }
        public override int GetHashCode()
        {
            int hashCode = -243844509;
            hashCode = hashCode * -1521134295 + StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            return hashCode;
        }
    }
}
