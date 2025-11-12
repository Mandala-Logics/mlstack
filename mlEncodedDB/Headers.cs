using DDEncoder;

namespace mlEncodedDB
{
    internal sealed class BlockTableEntry : IEncodable
    {
        public readonly static int EncodedSize = new BlockTableEntry(0L, 0, 0, true).GetEncodedSize();

        public long StartPos { get; }
        public int Length { get; }
        public bool Empty { get; set; }
        public int NextBlock { get; set; }
        public long EndPos => StartPos + Length;

        internal BlockTableEntry(long start, int len, int nextBlock, bool empty)
        {
            StartPos = start;
            Length = len;
            NextBlock = nextBlock;
            Empty = empty;
        }

        public BlockTableEntry(EncodedObject eo)
        {
            StartPos = eo.Next<long>();
            Length = eo.Next<int>();
            Empty = eo.Next<bool>();
            NextBlock = eo.Next<int>();
        }

        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(StartPos);
            encodedObj.Append(Length);
            encodedObj.Append(Empty);
            encodedObj.Append(NextBlock);
        }
    }
}