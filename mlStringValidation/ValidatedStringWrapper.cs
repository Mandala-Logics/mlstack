using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace mlStringValidation
{
    internal sealed class ValidatedStringWrapper : IValidatedString
    {
        //PUBLIC PROPERTIES
        public StringTemplate Template => baseString.Template;
        public string String => baseString.String;
        public int Length => baseString.String.Length;
        public char this[int index] => baseString.String[index];

        //PRIVATE PROPERTIES
        ValidatedString baseString;

        //CONSTRCUTORS
        internal ValidatedStringWrapper(ValidatedString vs)
        {
            baseString = vs;
        }

        //PUBLIC METHODS
        public int GetByteLength() => baseString.GetByteLength();
        public byte[] GetFixedBytes(int byteLength) => baseString.GetFixedBytes(byteLength);
        public byte[] GetFixedBytes() => baseString.GetFixedBytes(Encoding.UTF8.GetMaxByteCount(baseString.String.Length));
        public void Set(string newString) => baseString.Set(newString);
        public bool TrySet(string newString) => baseString.TrySet(newString);
        public override string ToString() => baseString.ToString();
        public void Set(IEnumerable<byte> bytes) => baseString.Set(bytes);
        public bool TrySet(IEnumerable<byte> bytes) => baseString.TrySet(bytes);

        //INTERFACE PROPERTIES
        public IEnumerator<char> GetEnumerator() => ((IValidatedString)baseString).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IValidatedString)baseString).GetEnumerator();        

        //STATIC OPERATORS
        public static implicit operator string(ValidatedStringWrapper vsw) => vsw.baseString;
    }
}
