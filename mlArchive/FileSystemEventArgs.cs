using System;
using System.Collections.Generic;
using System.Text;
using DDLib;
using mlStringValidation.Path;

namespace ArcV4
{
    public delegate void FileSystemChangeEventHandler(object sender, FileSystemChangeEventArgs args);

    public sealed class FileSystemChangeEventArgs : EventArgs
    {
        public PathChangeType Type { get; }
        public FileTableEntry Entry { get; }

        public FileSystemChangeEventArgs(PathChangeType type, FileTableEntry entry)
        {
            Type = type;
            Entry = entry ?? throw new ArgumentNullException("entry");
        }
    }
}
