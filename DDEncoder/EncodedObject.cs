using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DDEncoder
{
    public enum EncodingMode { Read, Write }

    public sealed class EncodedObject : EncodedValue
    { 
        //PUBLIC PROPERTIES
        public override EncodedType Type => EncodedType.Object;
        public int InitialCount { get; }
        public int CurrentCount => stack.Count;
        public bool EndOfQueue => CurrentCount == 0;
        public EncodingMode Mode { get; }
        public override int ID { get; }
        public override bool IsFixedSize => stack.All((ev) => { return ev.IsFixedSize; });
        public override int EncodedSize
        {
            get
            {
                int ret = 0;

                foreach (EncodedValue ev in stack) { ret += ev.EncodedSize; }

                return 4 + 4 + ret + stack.Count; //id + count + ret + typecodes
            }
        }

        //PRIVATE PROPERTIES
        private readonly Stack<EncodedValue> stack;

        //CONSTRUCTORS
        private EncodedObject(int id, IEnumerable<EncodedValue> vals) : base(default(IEncodable))
        {
            stack = new Stack<EncodedValue>(vals);
            InitialCount = vals.Count();
            ID = id;
            Mode = EncodingMode.Read;

            RegisteredType re;

            re = DDEncoder.GetRegisteredType(id);

            Value = re.Construct(this);
        }
        internal EncodedObject(IEncodable obj) : base(obj)
        {
            stack = new Stack<EncodedValue>();
            InitialCount = 0;
            ID = obj.GetEncoderID();
            Mode = EncodingMode.Write;
        }

        //PUBLIC IO FUNCTIONS
        public override int Write(Stream stream)
        {
            if (!stream?.CanWrite ?? true) throw new EncodingException("Stream is null or unable to write to stream.", EncodingExceptionReason.FailedToWriteStream);

            int w;

            try
            {
                w = stream.Write(ID);
                w += stream.Write(stack.Count);

                if (stack.Count == 0) { return w; }

                foreach (EncodedValue val in stack) 
                {
                    w += stream.Write(val.Type);
                    w += val.Write(stream);
                }
            }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToWriteStream, e); }

            return w;
        }
        public static int Read(Stream stream, out EncodedValue obj)
        {
            if ((stream is null) || !stream.CanRead) throw new EncodingException("Stream is null or unable to read from stream.", EncodingExceptionReason.FailedToWriteStream);

            int r, id, n;
            EncodedValue[] vals;

            try
            {
                r = stream.ReadInt(out id);

                if (!DDEncoder.IsRegistered(id)) throw new EncodingException($"ID read ({id}) was not registed.", EncodingExceptionReason.WrongImplimentation);

                r += stream.ReadInt(out int len);

                if (len < 0) throw new EncodingException($"Value count {len} cannot be less than zero.", EncodingExceptionReason.BadBinaryHeader);

                vals = new EncodedValue[len];

                for (n = 0; n < len; n++)
                {
                    r += stream.ReadEncodedType(out EncodedType et);

                    if (et == EncodedType.Array) { r += EncodedArray.Read(stream, out vals[n]); }
                    else if (et == EncodedType.Object) { r += Read(stream, out vals[n]); }
                    else { r += Read(stream, et, out vals[n]); }
                }
            }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToReadStream, e); }

            obj = new EncodedObject(id, vals);

            return r;
        }        

        //PUBLIC READ FUNCTIONS
        public EncodedType PeekType()
        {
            if (EndOfQueue) throw new EncodingException(EncodingExceptionReason.IncorrectValueCount);

            return stack.Peek().Type;
        }
        public object Peek()
        {
            if (EndOfQueue) throw new EncodingException(EncodingExceptionReason.IncorrectValueCount);

            return stack.Peek().Value;
        }
        public EncodedValue Next()
        {
            if (Mode == EncodingMode.Write) throw new EncodingException("Cannot read from shell which is in write mode.", EncodingExceptionReason.WrongIOMode);
            else if (EndOfQueue) throw new EncodingException(EncodingExceptionReason.IncorrectValueCount);

            return stack.Pop();
        }
        public T Next<T>()
        {
            if (Mode == EncodingMode.Write) throw new EncodingException("Cannot read from shell which is in write mode.", EncodingExceptionReason.WrongIOMode);
            else if (EndOfQueue) throw new EncodingException(EncodingExceptionReason.IncorrectValueCount);

            var obj = stack.Pop();
            var requestedType = typeof(T);
            var readType = obj.Value.GetType();

            if (requestedType.IsEnum)
            {
                if (requestedType.GetEnumUnderlyingType() == readType && Enum.IsDefined(requestedType, obj.Value)) 
                {
                    return (T)obj.Value;
                }
                else
                {
                    throw new EncodingException($"Cannot cast the next EncodedValue to this type ({requestedType.Name}) because the read type" +
                        $" ({readType.Name}) is not the same as the underlying enum type ({requestedType.GetEnumUnderlyingType().Name}) or the " +
                        $" value read ({obj.Value}) is not defined in the enum.",
                    EncodingExceptionReason.UnexpctedType);
                }
            }
            else if (obj.Value is T ret)
            {
                return ret;
            }
            else
            {
                throw new EncodingException($"Cannot cast the next EncodedValue to this type ({requestedType.Name}) because it is of type {readType.Name}.",
                    EncodingExceptionReason.UnexpctedType);
            }
        }

        //PUBLIC WRITE FUNCTIONS
        public void Append(EncodedValue val)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            if (val is null) throw new ArgumentNullException("val");

            stack.Push(val);
        }
        public void Append(Enum val)
        {
            if (val is null) throw new ArgumentNullException("val");

            var tc = System.Type.GetTypeCode(Enum.GetUnderlyingType(val.GetType()));

            var obj = Convert.ChangeType(val, tc);

            switch (tc)
            {
                case TypeCode.Byte:
                    Append((byte)obj);
                    break;
                case TypeCode.SByte:
                    Append((sbyte)obj);
                    break;
                case TypeCode.UInt16:
                    Append((ushort)obj);
                    break;
                case TypeCode.UInt32:
                    Append((uint)obj);
                    break;
                case TypeCode.UInt64:
                    Append((ulong)obj);
                    break;
                case TypeCode.Int16:
                    Append((short)obj);
                    break;
                case TypeCode.Int32:
                    Append((int)obj);
                    break;
                case TypeCode.Int64:
                    Append((long)obj);
                    break;
                default:
                    throw new ArgumentException("Enum underlying type is not one of the types it's supposed to be?");
            }
        }
        public void Append(bool obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(byte obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(string obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(char obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(DateTime obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(decimal obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(double obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(short obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(int obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(long obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(sbyte obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(float obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(ushort obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(uint obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(ulong obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedValue(obj));
        }
        public void Append(IEncodable obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(GetEncodedObject(obj));
        }
        public void Append(IEnumerable<EncodedValue> val)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(val));
        }
        public void Append(IEnumerable<int> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<byte> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<long> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<short> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<uint> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<ulong> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<ushort> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<sbyte> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<float> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<double> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<decimal> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<DateTime> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<string> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<bool> obj) 
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append(IEnumerable<char> obj)
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(new EncodedArray(obj));
        }
        public void Append<T>(IEnumerable<T> obj) where T : IEncodable
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(EncodedArray.GetEncodedArray(obj));
        }
        public void Append<T>(T[] obj) where T : IConvertible
        {
            if (Mode != EncodingMode.Write) throw new EncodingException("Cannot write to shell which is in read mode.", EncodingExceptionReason.WrongIOMode);

            stack.Push(EncodedArray.GetEncodedArray(obj));
        }

        //STATIC FUNCTIONS
        public static EncodedObject GetEncodedObject(IEncodable obj)
        {
            if (obj is null) throw new ArgumentNullException("obj");

            var eo = new EncodedObject(obj);

            try { obj.Encode(ref eo); }
            catch (Exception e) { throw new EncodingException("Error encoding object of type " + obj.GetType(), EncodingExceptionReason.Other, e); }

            if (eo is null) throw new EncodingException("Encoded object passed back out from Encode() cannot be null.", EncodingExceptionReason.Other);

            return eo;
        }
    }
}
