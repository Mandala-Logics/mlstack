using System;
using DDEncoder;

namespace ArcV4
{
    internal static class ArchiveConstants
    {
        public readonly static int ArchiveVerion = 4;

        public readonly static int FileHeaderLength = new ArchiveFileHeader(null, "temp").GetEncodedSize();
        public readonly static int BlockTableEntryLength = new BlockTableEntry(-1, BlockType.Empty, 1000, MinBlockLength).GetEncodedSize();

        public static readonly int BlockTableBlockCount = 64;
        public readonly static int BlockTableSize = BlockTableEntryLength * BlockTableBlockCount;

        public readonly static int MaxFileNameLength = 128;

        public readonly static int DefualtBlockLength = 4096;
        public readonly static int MinBlockLength = 512;
        public readonly static int MaxBlockLength = 10 * (int)Math.Pow(1024, 2);

        public readonly static int DefualtFileTableLength = 2048;
    }
}
