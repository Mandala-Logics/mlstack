using System;
using System.Collections.Generic;
using System.Threading;
using DDEncoder;
using mlFileInterface;

namespace ArcV4
{
    internal enum BlockType : byte { Empty = 0, BlockTable = 1, Binary = 2, FileTable = 3 }

    internal sealed class BlockTableEntry : IEncodable, IEquatable<BlockTableEntry>, IEquatable<mlFileInterface.IORange>
    {
        //PUBLIC PROPERTIES
        public BlockType Type { get; private set; }
        public bool IsEmpty => Type == BlockType.Empty;
        public bool IsBlockTable => Type == BlockType.BlockTable;
        public bool IsFileTable => Type == BlockType.FileTable;
        public bool IsBinary => Type == BlockType.Binary;
        public bool HasNext => NextBlock != -1;
        public mlFileInterface.IORange BlockRange { get; }
        public int BlockID => BlockRange.Key;
        public long UsedBytes { get; private set; }
        public int NextBlock { get; private set; }
        public int MaxBlocks => Math.DivRem((int)BlockRange.Length, ArchiveConstants.BlockTableEntryLength, out _);
        public bool ShouldWrite { get; private set; } = false;
        public IOTask WriteTask {get;set;}

        //CONSTRCUTORS
        internal BlockTableEntry(int blockID, BlockType type, long blockStart, long blockLength)
        {
            if (blockLength < ArchiveConstants.MinBlockLength) blockLength = ArchiveConstants.MinBlockLength;
            else if (blockLength > ArchiveConstants.MaxBlockLength) blockLength = ArchiveConstants.MaxBlockLength;

            if (blockStart < ArchiveConstants.FileHeaderLength) throw new ArgumentOutOfRangeException($"BlockStart position ({blockStart}) is too low.");

            BlockRange = new mlFileInterface.IORange(blockStart, blockLength, blockID);
            UsedBytes = 0;
            Type = type;
            NextBlock = -1;
            ShouldWrite = true;
        }

        //INTERNAL METHODS
        internal void SetNextBlock(int index)
        {
            if (index == NextBlock) { return; }
            else if (NextBlock != -1) throw new InvalidOperationException("Next block is already set.");
            else if (index <= 1) throw new ArgumentOutOfRangeException("Next block index cannot be one or less.");

            NextBlock = index;
            ShouldWrite = true;
        }
        internal void AllocateBlock(BlockType type)
        {
            if (type == BlockType.Empty) throw new ArgumentException("type cannot be empty when allocating.");
            else if (type == Type) { return; }

            Type = type;
            ShouldWrite = true;
        }
        internal void DeallocateBlock()
        {
            if (Type != BlockType.Empty) { ShouldWrite = true; }

            NextBlock = -1;
            Type = BlockType.Empty;
            UsedBytes = 0;            
        }
        internal void SetUsedBytes(long usedBytes)
        {
            if (usedBytes == UsedBytes) { return; }
            else if (usedBytes < 0) throw new ArgumentOutOfRangeException($"UsedBytes ({usedBytes}) cannot be less than zero.");

            if (BlockRange.Length < usedBytes)
            {
                throw new ArgumentOutOfRangeException($"UsedBytes ({usedBytes}) is outside of block range: {BlockRange}.");
            }
            else
            {
                UsedBytes = usedBytes;
                ShouldWrite = true;
            }
        }
        internal void EndChainHere()
        {
            if (NextBlock == -1) { return; }
            else
            {
                NextBlock = -1;
                ShouldWrite = true;
            }            
        }
        internal void ReportWritten() => ShouldWrite = false;

        //ENCODING
        public BlockTableEntry(EncodedObject encodedObj)
        {
            Type = encodedObj.Next<BlockType>();
            BlockRange = encodedObj.Next<mlFileInterface.IORange>();
            UsedBytes = encodedObj.Next<long>();
            NextBlock = encodedObj.Next<int>();

            if (NextBlock < -1 || NextBlock == 0) throw new BadBinaryException("NextBlock invalid.");
            if (BlockRange.StartPosition <= 0L) throw new BadBinaryException("BlockStart < 0.");
            if (BlockRange.StartPosition <= 0L) throw new BadBinaryException("BlockLength < 0.");
            if (UsedBytes < 0L) throw new BadBinaryException("UsedBytes < 0.");
        }
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(Type);
            encodedObj.Append(BlockRange);
            encodedObj.Append(UsedBytes);
            encodedObj.Append(NextBlock);
        }

        //OBJECT OVERRIDES
        public bool Equals(mlFileInterface.IORange range) => BlockRange.Equals(range);
        public override bool Equals(object obj) => obj is BlockTableEntry entry && Equals(entry);
        public bool Equals(BlockTableEntry other) => other is BlockTableEntry && BlockRange.Equals(other.BlockRange);
        public override int GetHashCode() => -381982585 + BlockRange.GetHashCode();
        public override string ToString() => $"Block: {BlockRange.Key}";
    }
}
