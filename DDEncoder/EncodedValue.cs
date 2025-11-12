using System;
using System.IO;
using System.Text;

namespace DDEncoder
{
    public unsafe class EncodedValue
    {
        public static EncodedValue Empty = new EncodedValue(0);

        //PUBLIC PROPERTIES
        public object Value { get; internal protected set; }         
        public virtual EncodedType Type
        {
            get
            {
                if (type is null)
                { 
                    return (EncodedType)(type = Value.GetType().GetEncodedType());
                }
                else { return (EncodedType)type; }
            }
        }
        public bool IsArray => Type == EncodedType.Array;
        public virtual EncodedType ElementType => EncodedType.Empty;
        public virtual bool IsFixedSize => HasFixedSize(Type);
        public virtual int EncodedSize
        {
            get
            {
                if (Type == EncodedType.String) { return bytes.Length + 4; }
                else { return bytes.Length; }
            }
        }
        public virtual int ID => 0;

        //PRIVATE FIELDS
        private EncodedType? type;
        private readonly byte[] bytes;

        //PUBLIC CONSTRUCTOS
        public EncodedValue(IConvertible obj)
        {
            Value = obj;
            
            switch (obj.GetType().GetEncodedType())
            {
                case EncodedType.Boolean:                    
                    bytes = BitConverter.GetBytes((bool)obj);
                    break;
                case EncodedType.Byte:
                    bytes = new byte[] { (byte)obj };
                    break;
                case EncodedType.Char:
                    bytes = BitConverter.GetBytes((char)obj);
                    break;
                case EncodedType.DateTime:
                    bytes = BitConverter.GetBytes(((DateTime)obj).ToBinary());
                    break;
                case EncodedType.Decimal:
                    var d = (decimal)obj;
                    bytes = Getbytes(&d, sizeof(decimal));
                    break;
                case EncodedType.Double:
                    bytes = BitConverter.GetBytes((double)obj);
                    break;
                case EncodedType.Int16:
                    bytes = BitConverter.GetBytes((short)obj);
                    break;
                case EncodedType.Int32:
                    bytes = BitConverter.GetBytes((int)obj);
                    break;
                case EncodedType.Int64:
                    bytes = BitConverter.GetBytes((long)obj);
                    break;
                case EncodedType.SByte:
                    sbyte x = (sbyte)obj;
                    bytes = Getbytes(&x, sizeof(sbyte));
                    break;
                case EncodedType.Single:
                    bytes = BitConverter.GetBytes((float)obj);
                    break;
                case EncodedType.String:
                    bytes = Encoding.UTF8.GetBytes((string)obj);
                    break;
                case EncodedType.UInt16:
                    bytes = BitConverter.GetBytes((ushort)obj);
                    break;
                case EncodedType.UInt32:
                    bytes = BitConverter.GetBytes((uint)obj);
                    break;
                case EncodedType.UInt64:
                    bytes = BitConverter.GetBytes((ulong)obj);
                    break;
                default:
                    throw new InvalidOperationException($"Internal error: this type code is not supported: {obj.GetType().GetEncodedType()}.");
            }
        }
        public EncodedValue(bool obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(bool));
        }
        public EncodedValue(byte obj)
        {
            Value = obj;

            bytes = new byte[1] { obj }; 
        }
        public EncodedValue(string obj)
        {
            if (string.IsNullOrEmpty(obj))
            {
                Value = string.Empty;

                bytes = new byte[0];
            }
            else
            {
                Value = obj;                

                bytes = Encoding.UTF8.GetBytes(obj);
            }
        }
        public EncodedValue(char obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(char));
        }
        public EncodedValue(DateTime obj)
        {
            Value = obj;

            long val = obj.ToBinary();

            bytes = Getbytes(&val, sizeof(long));
        }
        public EncodedValue(decimal obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(decimal));
        }
        public EncodedValue(double obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(double));
        }
        public EncodedValue(short obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(short));
        }
        public EncodedValue(int obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(int));
        }
        public EncodedValue(long obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(long));
        }
        public EncodedValue(sbyte obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(sbyte));
        }
        public EncodedValue(float obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(float));
        }
        public EncodedValue(ushort obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(ushort));
        }
        public EncodedValue(uint obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(uint));
        }
        public EncodedValue(ulong obj)
        {
            Value = obj;

            bytes = Getbytes(&obj, sizeof(ulong));
        }

        //PRIVATE CONTRUCTORS
        private EncodedValue(byte[] bytes, EncodedType et)
        {
            this.bytes = bytes;

            switch (et)
            {
                case EncodedType.Boolean:
                    Value = BitConverter.ToBoolean(bytes, 0);
                    break;
                case EncodedType.Byte:
                    Value = bytes[0];
                    break;
                case EncodedType.Char:
                    Value = BitConverter.ToChar(bytes, 0);
                    break;
                case EncodedType.DateTime:
                    Value = DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
                    break;
                case EncodedType.Decimal:
                    decimal dec = 0;
                    Setbytes(bytes, &dec, sizeof(decimal));
                    Value = dec;
                    break;
                case EncodedType.Double:
                    Value = BitConverter.ToDouble(bytes, 0);
                    break;
                case EncodedType.Int16:
                    Value = BitConverter.ToInt16(bytes, 0);
                    break;
                case EncodedType.Int32:
                    Value = BitConverter.ToInt32(bytes, 0);
                    break;
                case EncodedType.Int64:
                    Value = BitConverter.ToInt64(bytes, 0);
                    break;
                case EncodedType.SByte:
                    sbyte sb = 0;
                    Setbytes(bytes, &sb, sizeof(sbyte));
                    Value = sb;
                    break;
                case EncodedType.Single:
                    Value = BitConverter.ToSingle(bytes, 0);
                    break;
                case EncodedType.String:
                    if (bytes.Length > 0) { Value = Encoding.UTF8.GetString(bytes); }
                    else { Value = string.Empty; }
                    break;
                case EncodedType.UInt16:
                    Value = BitConverter.ToUInt16(bytes, 0);
                    break;
                case EncodedType.UInt32:
                    Value = BitConverter.ToUInt32(bytes, 0);
                    break;
                case EncodedType.UInt64:
                    Value = BitConverter.ToUInt64(bytes, 0);
                    break;
                default:
                    throw new InvalidOperationException($"Internal error: this type code is not supported: {et}.");
            }
        }

        //PROCTED CONTRUCTORS
        protected EncodedValue(object value) { Value = value; }

        //PUBLIC FUNCTIONS
        public virtual int Write(Stream stream)
        {
            if (!stream?.CanWrite ?? true) throw new EncodingException("Stream is null or unable to write to stream.", EncodingExceptionReason.FailedToWriteStream);

            if (Type == EncodedType.String)
            {
                int x;
                
                try
                {
                    x = stream.Write(bytes.Length);

                    if (bytes.Length > 0) stream.Write(bytes, 0, bytes.Length);
                }
                catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToWriteStream, e); }

                return bytes.Length + x;
            }
            else
            {
                try { stream.Write(bytes, 0, bytes.Length); }
                catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToWriteStream, e); }

                return bytes.Length;
            }
        }

        //OPERATORS
        public override string ToString() => Value.ToString();
        public override int GetHashCode()
        {
            unchecked { return Value.GetHashCode() * 19 + bytes.GetHashCode(); }
        }
        public override bool Equals(object obj)
        {
            if (obj is EncodedValue ev)
            {
                return bytes.Equals(ev.bytes);
            }
            else
            {
                return false;
            }
        }

        //STATIC FUNCTIONS
        public static int Read(Stream stream, EncodedType type, out EncodedValue val)
        {
            if (!stream?.CanRead ?? true) throw new EncodingException("Stream is null or unable to read from stream.", EncodingExceptionReason.FailedToReadStream);

            byte[] b;
            int len;
            int r;

            if (type == EncodedType.String)
            {
                try 
                { 
                    r = stream.ReadInt(out len);

                    if (len < 0) throw new EncodingException("String length less than zero.", EncodingExceptionReason.BadBinaryHeader);

                    b = new byte[len];

                    if (len > 0) 
                    {
                        r += len = stream.Read(b, 0, len);
                        if (len < b.Length) throw new EncodingException("Failed to read enough bytes from stream.", EncodingExceptionReason.FailedToReadStream);
                    }
                    
                }
                catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToReadStream, e); }                
            }
            else
            {
                len = TypeCodeSize(type);

                if (len == 0) throw new EncodingException("This type is not supported and cannot be read: " + type, EncodingExceptionReason.BadBinaryHeader);

                b = new byte[len];

                try { r = len = stream.Read(b, 0, len); }
                catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToReadStream, e); }

                if (len < b.Length) throw new EncodingException("Failed to read enough bytes from stream.", EncodingExceptionReason.FailedToReadStream);
            }

            val = new EncodedValue(b, type);

            return r;
        }
        protected static byte[] Getbytes(void* srcPtr, int len)
        {
            var b = new byte[len];

            fixed (byte* dstPtr = b)
            {
                Buffer.MemoryCopy(srcPtr, dstPtr, len, len);
            }

            return b;
        }
        protected static void Setbytes(byte[] src, void* dstPtr, int len)
        {
            fixed (byte* srcPtr = src)
            {
                Buffer.MemoryCopy(srcPtr, dstPtr, len, len);
            }
        }
        public static int TypeCodeSize(EncodedType et)
        {
            switch (et)
            {
                case EncodedType.Boolean:
                    return sizeof(bool);
                case EncodedType.Byte:
                    return sizeof(byte);
                case EncodedType.Char:
                    return sizeof(char);
                case EncodedType.DateTime:
                    return sizeof(long);
                case EncodedType.Decimal:
                    return sizeof(decimal);
                case EncodedType.Double:
                    return sizeof(double);
                case EncodedType.Int16:
                    return sizeof(short);
                case EncodedType.Int32:
                    return sizeof(int);
                case EncodedType.Int64:
                    return sizeof(long);
                case EncodedType.SByte:
                    return sizeof(sbyte);
                case EncodedType.Single:
                    return sizeof(float);
                case EncodedType.UInt16:
                    return sizeof(ushort);
                case EncodedType.UInt32:
                    return sizeof(uint);
                case EncodedType.UInt64:
                    return sizeof(ulong);
                default:
                    return 0;
            }
        }
        public static bool HasFixedSize(EncodedType et) => TypeCodeSize(et) != 0;
    }
}
