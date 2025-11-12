using System;
using System.Collections.Generic;
using System.Text;

namespace mlStringValidation
{
    public interface IValidatedString : IEnumerable<char>
    {
        char this[int index] { get; }
        int Length { get; }
        StringTemplate Template { get; }
        string String { get; }
        void Set(string newString);
        bool TrySet(string newString);
        int GetByteLength();
        byte[] GetFixedBytes(int byteLength);
        byte[] GetFixedBytes();
        void Set(IEnumerable<byte> bytes);
        bool TrySet(IEnumerable<byte> bytes);
    }
}
