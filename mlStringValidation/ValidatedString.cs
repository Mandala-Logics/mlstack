using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DDEncoder;

namespace mlStringValidation
{
    public sealed class ValidatedString : IEncodable, IValidatedString, ICloneable, IEquatable<string>, IEquatable<ValidatedString>
    {
        //EVENTS
        public event ValidatedStringChangeEventHandler BeforeClear;
        public event ValidatedStringChangeEventHandler BeforeChange;
        public event ValidatedStringEventHandler AfterChange;
        public event ValidatedStringEventHandler SetChanged;

        //PUBLIC PROPERTIES
        public StringTemplate Template { get; }
        public int Length => String.Length;
        public string String => string.IsNullOrEmpty(setString) ? Default : setString;
        public string Default { get; } = string.Empty;
        public bool IsSet => !string.IsNullOrEmpty(String);
        public char this[int index] => String[index];
        public int MaxByteLength {get;} = -1;

        //PRIVATE PROPERTIES
        private string setString;

        //CONSTRUCTORS
        public ValidatedString(EncodedObject eo)
        {
            Template = eo.Next<StringTemplate>();
            setString = eo.Next<string>();
            Default = eo.Next<string>();
        }
        public ValidatedString(StringTemplate temp)
        {
            Template = temp;
            setString = string.Empty;
            Default = string.Empty;
        }
        public ValidatedString(StringTemplate temp, int maxByteLength) : this(temp)
        {
            MaxByteLength = maxByteLength;
        }
        public ValidatedString(StringTemplate temp, string defualtString, IEnumerable<byte> bytes)
        {
            Template = temp;

            var s = Encoding.UTF8.GetString(bytes.ToArray());
            Set(s);

            Default = defualtString ?? string.Empty;
        }
        public ValidatedString(StringTemplate temp, IEnumerable<byte> bytes) : this(temp, string.Empty, bytes) { }
        public ValidatedString(StringTemplate temp, string defualtString)
        {
            Template = temp;
            setString = string.Empty;
            Default = defualtString ?? string.Empty;
        }
        public ValidatedString(StringTemplate temp, string defualtString, string initialValue)
        {
            Template = temp;
            Default = defualtString ?? string.Empty;
            Set(initialValue ?? string.Empty);
        }

        public ValidatedString(StringTemplate temp, string defualtString, int maxByteLength)
            : this(temp, defualtString)
        {
            MaxByteLength = maxByteLength;
        }
        public ValidatedString(StringTemplate temp, string defualtString, string initialValue, int maxByteLength)
            : this(temp, defualtString, initialValue)
        {
            MaxByteLength = maxByteLength;
        }

        //INTERFACE FUNCTIONS
        static ValidatedString() { DDEncoder.DDEncoder.RegisterType(typeof(ValidatedString)); }
        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(Template);
            encodedObj.Append(setString);
            encodedObj.Append(Default);
        }

        //PUBLIC FUNCTIONS
        public ValidatedString Copy() => new ValidatedString(Template, Default, setString);
        object ICloneable.Clone() => Copy();
        public IValidatedString AsInterface() => new ValidatedStringWrapper(this);
        public override string ToString() => string.IsNullOrEmpty(setString) ? Default : setString;
        public void Clear()
        {
            if (!IsSet) { return; }

            var e = new ValidatedStringBeforeChangeEventArgs(string.Empty);

            BeforeClear?.Invoke(this, e);

            if (e.Cancel) { return; }

            BeforeChange?.Invoke(this, e);

            if (e.Cancel) { return; }

            setString = Default;

            AfterChange?.Invoke(this);

            SetChanged?.Invoke(this);
        }
        public void Set(IEnumerable<byte> bytes)
        {
            var s = Encoding.UTF8.GetString(bytes.ToArray()).Trim('\0');
            Set(s);
        }
        public void Set(string newString)
        {
            var b = IsSet;

            newString = Template.Validate(newString);

            int x;

            if (MaxByteLength >= 0 && (x = Encoding.UTF8.GetByteCount(newString)) > MaxByteLength)
            {
                throw new StringValidationException($"String encoded length ({x}) is too long, max: {MaxByteLength}.", StringExceptionReason.TooManyBytes);
            }

            var e = new ValidatedStringBeforeChangeEventArgs(newString);

            BeforeChange?.Invoke(this, e);

            if (e.Cancel) { return; }

            setString = newString;

            AfterChange?.Invoke(this);

            if (b != IsSet) { SetChanged?.Invoke(this); }
        }
        public bool TrySet(string newString)
        {
            try { Set(newString); return true; }
            catch (StringValidationException) { return false; }
        }
        public bool TrySet(string newString, out StringValidationException sve)
        {
            try { Set(newString); sve = default; return true; }
            catch (StringValidationException e) { sve = e; return false; }
        }
        public bool TrySet(IEnumerable<byte> bytes)
        {
            try { Set(bytes); return true; }
            catch (StringValidationException) { return false; }
        }
        public byte[] GetFixedBytes(int byteLength)
        {
            var b = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(setString) ? Default : setString);

            if (b.Length > byteLength)
            {
                throw new InvalidCastException($"This string cannot be fully cast into {byteLength} bytes.");
            }
            else
            {
                var ret = new byte[byteLength];

                Buffer.BlockCopy(b, 0, ret, 0, b.Length);

                return ret;
            }            
        }
        public byte[] GetFixedBytes() => GetFixedBytes(Encoding.UTF8.GetMaxByteCount(String.Length));
        public int GetByteLength() => Encoding.UTF8.GetByteCount(string.IsNullOrEmpty(setString) ? Default : setString);

        //OBJECT OVERRIDES
        public bool Equals(string str, StringComparison comparison)
        {
            return str is string && String.Equals(str, comparison);
        }
        public bool Equals(string str)
        {
            return str is string && String.Equals(str);
        }
        public bool Equals(ValidatedString other)
        {
            return other != null &&
                   EqualityComparer<StringTemplate>.Default.Equals(Template, other.Template) &&
                   String == other.String;
        }
        public override bool Equals(object obj)
        {
            return obj is ValidatedString other &&
                   EqualityComparer<StringTemplate>.Default.Equals(Template, other.Template) &&
                   String == other.String;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Template, String);
        }

        //INTERFACE PROPERTIES
        public IEnumerator<char> GetEnumerator() => ((IEnumerable<char>)String).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)String).GetEnumerator();        

        //STATIC OPERATORS
        public static implicit operator string(ValidatedString vs) => string.IsNullOrEmpty(vs.setString) ? vs.Default : vs.setString;
        public static bool operator ==(ValidatedString left, ValidatedString right)
        {
            return EqualityComparer<ValidatedString>.Default.Equals(left, right);
        }
        public static bool operator !=(ValidatedString left, ValidatedString right)
        {
            return !(left == right);
        }
    }
}
