using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;

namespace DDEncoder
{
    public class DDEncoder : IDisposable
    {
        //STATIC PROPERTIES
        private static readonly Dictionary<int, RegisteredType> register = new Dictionary<int, RegisteredType>();
        public static readonly Type GenericEnumerableType = typeof(IEnumerable<>);

        //PUBLIC PROPERTIES
        public Stream BaseStream { get; }
        public bool OwnsStream { get; set; } = false;
        public bool CanRead => BaseStream.CanRead;
        public bool CanWrite => BaseStream.CanWrite;

        //CONSTRCUTORS
        public DDEncoder(Stream stream)
        {
            BaseStream = stream ?? throw new ArgumentNullException("stream");
            OwnsStream = false;
        }
        public DDEncoder(Stream stream, bool ownsStream)
        {
            BaseStream = stream ?? throw new ArgumentNullException("stream");
            OwnsStream = ownsStream;
        }
        static DDEncoder()
        {
            RegisterType(typeof(NullEncodable));
        }

        //PUBLIC READ FUNCTIONS
        public int Read(out EncodedValue val)
        {
            BaseStream.ReadEncodedType(out EncodedType et);

            switch (et)
            {
                case EncodedType.Object:
                    return EncodedObject.Read(BaseStream, out val);
                case EncodedType.Array:
                    return EncodedArray.Read(BaseStream, out val);
                case EncodedType.Boolean:
                case EncodedType.Char:
                case EncodedType.SByte:
                case EncodedType.Byte:
                case EncodedType.Int16:
                case EncodedType.UInt16:
                case EncodedType.Int32:
                case EncodedType.UInt32:
                case EncodedType.Int64:
                case EncodedType.UInt64:
                case EncodedType.Single:
                case EncodedType.Double:
                case EncodedType.Decimal:
                case EncodedType.DateTime:
                case EncodedType.String:
                    return EncodedValue.Read(BaseStream, et, out val);
                case EncodedType.Empty:
                default:
                    throw new EncodingException("Unrecognised type code read from stream.", EncodingExceptionReason.BadBinaryHeader);
            }
        }
        public int Read<T>(out T val)
        {
            int ret = Read(out EncodedValue ev);

            if (ev.Value is T x)
            {
                val = x;
            }
            else if (typeof(T).IsAssignableFrom(ev.Value.GetType()))
            {
                val = (T)ev.Value;                
            }
            else
            {
                throw new EncodingException($"The type requested ({typeof(T)}) did not match the type whcih was read ({ev.Value.GetType()}).", EncodingExceptionReason.UnexpctedType);
            }

            return ret;
        }
        public EncodedType PeekType()
        {
            if (!BaseStream.CanSeek) throw new EncodingException("Cannot peek if stream is unable to seek.");
            
            BaseStream.ReadEncodedType(out EncodedType et);

            BaseStream.Position--;

            return et;
        }

        //PUBLIC WRITE FUNCTIONS
        public int Write(EncodedValue obj)
        {
            BaseStream.Write(obj.Type);

            return obj.Write(BaseStream);
        }
        public int Write(IEncodable obj) => Write(obj.GetEncodedObject());
        public int Write(bool obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(byte obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(string obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(char obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(DateTime obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(decimal obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(double obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(short obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(int obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(long obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(sbyte obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(float obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(ushort obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(uint obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(ulong obj)
        {
            var ev = new EncodedValue(obj);

            BaseStream.Write(ev.Type);

            return ev.Write(BaseStream);
        }
        public int Write(IEnumerable<EncodedValue> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write<T>(IEnumerable<T> obj) where T : IEncodable
        {
            var ea = EncodedArray.GetEncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write<T>(T[] obj) where T : IConvertible
        {
            var ea = EncodedArray.GetEncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<int> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<byte> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<long> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<short> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<uint> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<ulong> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<ushort> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<sbyte> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<float> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<double> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<decimal> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<DateTime> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<string> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<bool> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public int Write(IEnumerable<char> obj)
        {
            var ea = new EncodedArray(obj);

            BaseStream.Write(EncodedType.Array);

            return ea.Write(BaseStream);
        }
        public void Flush() => BaseStream.Flush();

        //STATIC OBJECT FUNCTIONS
        public static int EncodeObject(Stream stream, IEncodable obj)
        {
            if (!stream?.CanWrite ?? true) throw new EncodingException("Stream is null or unable to write to stream.", EncodingExceptionReason.FailedToWriteStream);

            EncodedObject eo = new EncodedObject(obj);
            int w;

            try { obj.Encode(ref eo); }
            catch (Exception e) { throw new EncodingException("Failed to encode object of type " + obj.GetType(), EncodingExceptionReason.Other, e); }

            try { w = stream.Write(EncodedType.Object); }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToWriteStream, e); }

            w += eo.Write(stream);

            return w;
        }
        public static int DecodeObject<T>(Stream stream, out T obj)
        {
            if (!stream?.CanRead ?? true) throw new EncodingException("Stream is null or unable to read from stream.", EncodingExceptionReason.FailedToReadStream);

            // if (!IsRegistered(typeof(T))) throw new EncodingException("This type is not registered: " + typeof(T), EncodingExceptionReason.WrongImplimentation);

            int r;
            EncodedType et;

            try { r = stream.ReadEncodedType(out et); }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToReadStream, e); }

            if (et != EncodedType.Object) 
            { 
                throw new EncodingException($"Expected to read object typecode in header but read {et} type instead.", EncodingExceptionReason.BadBinaryHeader); 
            }

            r += EncodedObject.Read(stream, out EncodedValue ev);

            if (ev.Value is T ret)
            {
                obj = ret;
            }
            else
            {
                throw new EncodingException($"The object type was expcted to be {typeof(T)} but the object read was of type {ev.Value.GetType()}.", EncodingExceptionReason.UnexpctedType);
            }

            return r;
        }

        //STATIC CAST FUNCTIONS
        public static bool TryCastToEncoded(object obj, out EncodedValue val)
        {
            EncodedType et = obj.GetType().GetEncodedType();

            switch (et)
            {                
                case EncodedType.Object:

                    var type = obj.GetType();

                    if (obj is IEncodable a)
                    {
                        val = a.GetEncodedObject();
                        return true;
                    }
                    else if (obj is IEnumerable x)
                    {
                        type = x.GetType();

                        type = type.GetInterfaces().First( (t) => t.GetGenericTypeDefinition() == GenericEnumerableType );

                        var typeArgument = type.GetGenericArguments()[0];
                        var interfaces = typeArgument.GetInterfaces();
                        
                        if (interfaces.Contains(typeof(IConvertible)))
                        {
                            val = (EncodedValue)Activator.CreateInstance(typeof(EncodedArray), obj);

                        }
                        else if (interfaces.Contains(RegisteredType.InterfaceType))
                        {
                            val = (EncodedValue)EncodedArray.ArrayConversionMethod.MakeGenericMethod(typeArgument).Invoke(null, new object[] { obj });
                        }
                        else
                        {
                            val = default;
                            return false;
                        }
                        
                        return true;
                    }
                    else { val = null; return false; }
                    
                case EncodedType.Array:
                    if (TryCastArray(obj, out EncodedArray ea))
                    {
                        val = ea;
                        return true;
                    }
                    break;
                case EncodedType.Boolean:
                    val = new EncodedValue((bool)obj);
                    return true;
                case EncodedType.Char:
                    val = new EncodedValue((char)obj);
                    return true;
                case EncodedType.SByte:
                    val = new EncodedValue((sbyte)obj);
                    return true;
                case EncodedType.Byte:
                    val = new EncodedValue((byte)obj);
                    return true;
                case EncodedType.Int16:
                    val = new EncodedValue((short)obj);
                    return true;
                case EncodedType.UInt16:
                    val = new EncodedValue((ushort)obj);
                    return true;
                case EncodedType.Int32:
                    val = new EncodedValue((int)obj);
                    return true;
                case EncodedType.UInt32:
                    val = new EncodedValue((uint)obj);
                    return true;
                case EncodedType.Int64:
                    val = new EncodedValue((long)obj);
                    return true;
                case EncodedType.UInt64:
                    val = new EncodedValue((ulong)obj);
                    return true;
                case EncodedType.Single:
                    val = new EncodedValue((float)obj);
                    return true;
                case EncodedType.Double:
                    val = new EncodedValue((double)obj);
                    return true;
                case EncodedType.Decimal:
                    val = new EncodedValue((decimal)obj);
                    return true;
                case EncodedType.DateTime:
                    val = new EncodedValue((DateTime)obj);
                    return true;
                case EncodedType.String:
                    val = new EncodedValue((string)obj);
                    return true;                    
            }

            val = null;
            return false;
        }
        private static bool TryCastArray(object obj, out EncodedArray ea)
        {
            var type = obj.GetType().GetElementType();
            var et = type.GetEncodedType();

            switch (et)
            {                
                case EncodedType.Array:
                    
                    var arr = obj as Array;
                    var ret = new List<EncodedValue>(arr.Length);
                    EncodedValue tmp;

                    for (int c = 0; c < arr.Length; c++)
                    {
                        if (TryCastToEncoded(arr.GetValue(c), out tmp))
                        {
                            ret.Add(tmp);
                        }
                        else {break;}
                    }

                    ea = new EncodedArray(ret);
                    return true;

                case EncodedType.Boolean:
                    ea = new EncodedArray((IEnumerable<bool>)obj);
                    return true;
                case EncodedType.Char:
                    ea = new EncodedArray((IEnumerable<char>)obj);
                    return true;
                case EncodedType.SByte:
                    ea = new EncodedArray((IEnumerable<sbyte>)obj);
                    return true;
                case EncodedType.Byte:
                    ea = new EncodedArray((IEnumerable<byte>)obj);
                    return true;
                case EncodedType.Int16:
                    ea = new EncodedArray((IEnumerable<short>)obj);
                    return true;
                case EncodedType.UInt16:
                    ea = new EncodedArray((IEnumerable<ushort>)obj);
                    return true;
                case EncodedType.Int32:
                    ea = new EncodedArray((IEnumerable<int>)obj);
                    return true;
                case EncodedType.UInt32:
                    ea = new EncodedArray((IEnumerable<uint>)obj);
                    return true;
                case EncodedType.Int64:
                    ea = new EncodedArray((IEnumerable<long>)obj);
                    return true;
                case EncodedType.UInt64:
                    ea = new EncodedArray((IEnumerable<ulong>)obj);
                    return true;
                case EncodedType.Single:
                    ea = new EncodedArray((IEnumerable<float>)obj);
                    return true;
                case EncodedType.Double:
                    ea = new EncodedArray((IEnumerable<double>)obj);
                    return true;
                case EncodedType.Decimal:
                    ea = new EncodedArray((IEnumerable<decimal>)obj);
                    return true;
                case EncodedType.DateTime:
                    ea = new EncodedArray((IEnumerable<DateTime>)obj);
                    return true;
                case EncodedType.String:
                    ea = new EncodedArray((IEnumerable<string>)obj);
                    return true;                 
            }

            ea = null;
            return false;
        }

        //STATIC REGISTER FUNCTIONS
        public static void RegisterType(Type type)
        {
            if (!IsRegistered(type))
            {
                int id = DDHash.HashString32(type.FullName);

                if (register.ContainsKey(id)) throw new ArgumentException("The ID supplied for this type is not unique.");

                register.Add(id, new RegisteredType(type));
            }
        }
        public static void RegisterTypes(params Type[] types)
        {
            foreach (Type type in types) { RegisterType(type); }
        }
        public static RegisteredType GetRegisteredType(int id)
        {
            try
            {
                return register[id];
            }
            catch (KeyNotFoundException)
            {
                throw new EncodingException($"ID read ({id}) was not registed.", EncodingExceptionReason.WrongImplimentation);
            }
        }
        public static bool IsRegistered(int id) => register.ContainsKey(id);
        public static bool IsRegistered(Type type) => register.Any((kvp) => kvp.Value.Type.Equals(type));
        public void Dispose()
        {
            if (OwnsStream) { BaseStream?.Dispose(); }
        }
    }
}
