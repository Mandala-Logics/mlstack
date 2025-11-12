using System;
using System.Collections.Generic;
using System.Text;

namespace DDEncoder
{
    public enum EncodedType : byte
    {
        Empty = 0,
        Object = 1,
        Array = 2,
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10,
        Int64 = 11,
        UInt64 = 12,
        Single = 13,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18
    }

    public interface IEncodable
    {
        void Encode(ref EncodedObject encodedObj);
    }

    public sealed class NullEncodable : IEncodable
    {
        public NullEncodable() { }
        public NullEncodable(EncodedObject eo)
        {
            eo.Next<byte>();
        }
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(DDHash.ZeroByte);
        }
    }
}
