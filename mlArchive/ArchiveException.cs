using System;
using System.Collections.Generic;
using System.Text;

namespace ArcV4
{
    public class ArchiveException : Exception
    {
        public ArchiveException() { }
        public ArchiveException(string message) : base(message) { }
        public ArchiveException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class BadBinaryException : ArchiveException
    {
        public BadBinaryException() : base("The binary headers in this archive file appear to be corrupted.") { }
        public BadBinaryException(Exception innerException) : base("The binary headers in this archive file appear to be corrupted.", innerException) { }
        public BadBinaryException(string message) : base(message) { }
        public BadBinaryException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class ArchivePackagedException : ArchiveException
    {
        public ArchivePackagedException() : base("This archive is packaged and cannot be modified.") { }
    }

    public sealed class BadBlockID : ArchiveException
    {
        public BadBlockID() : base("The block ID is out of range.") { }
        public BadBlockID(Exception innerException) : base("The block ID is out of range.", innerException) { }
        public BadBlockID(int blockID) : base($"This blockID ({blockID}) is out of range.") { }
        public BadBlockID(int blockID, Exception innerException) : base($"This blockID ({blockID}) is out of range.", innerException) { }
    }

    public sealed class FileIsOpenException : ArchiveException
    {
        public FileIsOpenException() : base("A file is open and cannot be deleted or opened.") { }
        public FileIsOpenException(Exception innerException) : base("A file is open and cannot be deleted or opened.", innerException) { }
        public FileIsOpenException(string file) : base($"This file ({file}) is open and cannot be deleted or opened.") { }
        public FileIsOpenException(string file, Exception innerException) : base($"This file ({file}) is open and cannot be deleted or opened.", innerException) { }
    }

    public class InvalidNameException : ArchiveException
    {
        public InvalidNameException() { }
        public InvalidNameException(string message) : base(message) { }
        public InvalidNameException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class NameNotAllowedException : InvalidNameException
    {
        public NameNotAllowedException() : base("This name is not allowed.") { }
        public NameNotAllowedException(Exception innerException) : base("This name is not allowed.", innerException) { }
        public NameNotAllowedException(string name) : base($"The name '{name}' is not allowed.") { }
        public NameNotAllowedException(string name, Exception innerException) : base($"The name '{name}' is not allowed.", innerException) { }
    }

    public sealed class NameAlreadyTakenException : InvalidNameException
    {
        public NameAlreadyTakenException() : base("This name is already in use.") { }
        public NameAlreadyTakenException(Exception innerException) : base("This name is already in use.", innerException) { }
        public NameAlreadyTakenException(string name) : base($"The name '{name}' is already in use.") { }
        public NameAlreadyTakenException(string name, Exception innerException) : base($"The name '{name}' is already in use.", innerException) { }
    }

    public sealed class CannotModifyRootDirException : ArchiveException
    {
        public CannotModifyRootDirException() : base("Cannot make this change to the root folder.") { }
    }

    public class FileSystemException : ArchiveException
    {
        public FileSystemException() { }
        public FileSystemException(string message) : base(message) { }
        public FileSystemException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class EntryDeletedException : FileSystemException
    {
        public EntryDeletedException() : base("This file/dir is deleted.") { }
        public EntryDeletedException(Exception innerException) : base("This file/dir is deleted.", innerException) { }
        public EntryDeletedException(string name) : base($"The file table entry {name} is deleted.") { }
        public EntryDeletedException(string name, Exception innerException) : base($"The file table entry {name} is deleted.", innerException) { }
    }

    public sealed class EntryNotFoundException : FileSystemException
    {
        public EntryNotFoundException() : base("This file/dir was not found.") { }
        public EntryNotFoundException(Exception innerException) : base("This file/dir was not found.", innerException) { }
        public EntryNotFoundException(string name) : base($"The file table entry '{name}' could not be found.") { }
        public EntryNotFoundException(string name, Exception innerException) : base($"The file table entry '{name}' could not be found.", innerException) { }
    }

    public sealed class ArchiveEncryptedException : ArchiveException
    {
        public ArchiveEncryptedException()
        {
        }

        public ArchiveEncryptedException(string message) : base(message)
        {
        }

        public ArchiveEncryptedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
