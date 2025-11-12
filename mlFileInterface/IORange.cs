using System;
using DDEncoder;

namespace mlFileInterface
{
    public readonly struct IORange : IEncodable, IEquatable<IORange>
    {
        //PUBLIC PROPERTIES
        public long StartPosition { get; }
        public long Length { get; }
        public long EndPosition => StartPosition + Length;
        public int Key { get; }

        //CONSTRCUTORS
        public IORange(long startPosition, long length)
        {
            if (length < 0L) throw new ArgumentOutOfRangeException("Length cannot be less than zero.");
            if (startPosition < 0L) throw new ArgumentOutOfRangeException("StartPosition cannot be less than zero.");

            StartPosition = startPosition;
            Length = length;
            Key = -1;
        }
        public IORange(long startPosition, long length, int key)
        {
            if (length < 0L) throw new ArgumentOutOfRangeException("Length cannot be less than zero.");
            if (startPosition < 0L) throw new ArgumentOutOfRangeException("StartPosition cannot be less than zero.");

            StartPosition = startPosition;
            Length = length;
            Key = key;
        }

        //STATIC CONTRUCTOR
        static IORange()
        {
            DDEncoder.DDEncoder.RegisterType(typeof(IORange));
        }

        //ENCODING
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(StartPosition);
            encodedObj.Append(Length);
            encodedObj.Append(Key);
        }
        public IORange(EncodedObject encodedObj)
        {
            StartPosition = encodedObj.Next<long>();
            Length = encodedObj.Next<long>();
            Key = encodedObj.Next<int>();
        }

        //OBJECT OVERRIDES
        public override string ToString() => $"({StartPosition}, {Length})";
        public override bool Equals(object obj) => obj is IORange range && Equals(range);
        public bool Equals(IORange other)
        {
            return StartPosition == other.StartPosition &&
                   Length == other.Length;
        }
        public override int GetHashCode()
        {
            int hashCode = -789397647;
            hashCode = hashCode * -1521134295 + StartPosition.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        } 
        public static bool operator ==(IORange left, IORange right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(IORange left, IORange right)
        {
            return !(left == right);
        }
    }
}
