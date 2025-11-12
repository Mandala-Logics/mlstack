using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DDEncoder
{
    public sealed class EncodedArray : EncodedValue
    {
        //STAIC PROPERTIES
        public static MethodInfo ArrayConversionMethod = 
            typeof(EncodedArray).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where((m) =>
            {
                return m.Name == "GetEncodedArray"
                && m.GetGenericArguments()[0].GetGenericParameterConstraints().Contains(typeof(IEncodable));
            } ).First();

        //PUBLIC PROPERTIES
        public override EncodedType Type => EncodedType.Array;
        public override EncodedType ElementType => elementType;
        public override bool IsFixedSize => elementType != EncodedType.String && elementType != EncodedType.Object;
        public override int EncodedSize
        {
            get
            {
                int ret = 0;

                foreach (EncodedValue ev in elements) { ret += ev.EncodedSize; }

                return 1 + 4 + ret; //element type + length + ret
            }
        }
        public override int ID {get;}

        //PRIVATE PROPERTIES
        private readonly EncodedType elementType;
        private readonly List<EncodedValue> elements;

        //CONSTRUCTORS
        public EncodedArray(IEnumerable<int> val) : base(val) 
        {
            elementType = EncodedType.Int32;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (int x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<byte> val) : base(val)
        {
            elementType = EncodedType.Byte;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (byte x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<long> val) : base(val)
        {
            elementType = EncodedType.Int64;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (long x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<short> val) : base(val)
        {
            elementType = EncodedType.Int16;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (short x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<uint> val) : base(val)
        {
            elementType = EncodedType.UInt32;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (uint x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<ulong> val) : base(val)
        {
            elementType = EncodedType.UInt64;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (ulong x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<ushort> val) : base(val)
        {
            elementType = EncodedType.UInt16;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (ushort x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<sbyte> val) : base(val)
        {
            elementType = EncodedType.SByte;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (sbyte x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<float> val) : base(val)
        {
            elementType = EncodedType.Single;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (float x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<double> val) : base(val)
        {
            elementType = EncodedType.Double;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (double x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<decimal> val) : base(val)
        {
            elementType = EncodedType.Decimal;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (decimal x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<DateTime> val) : base(val)
        {
            elementType = EncodedType.DateTime;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (DateTime x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<string> val) : base(val)
        {
            elementType = EncodedType.String;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (string x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<bool> val) : base(val)
        {
            elementType = EncodedType.Boolean;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (bool x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        public EncodedArray(IEnumerable<char> val) : base(val)
        {
            elementType = EncodedType.Char;

            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0) { elements = new List<EncodedValue>(); }
            else
            {
                elements = new List<EncodedValue>(val.Count());
                foreach (char x in val) { elements.Add(new EncodedValue(x)); }
            }
        }
        private EncodedArray(IEnumerable<IEncodable> val, object trueVal, int id) : base(trueVal)
        {
            elementType = EncodedType.Object;
            ID = id;

            elements = new List<EncodedValue>(val.Count());
            foreach (IEncodable x in val) { elements.Add(x.GetEncodedObject()); }
        }
        private EncodedArray(IEnumerable<IConvertible> val, object trueVal, Type type) : base(trueVal)
        {
            elementType = type.GetEncodedType();

            elements = new List<EncodedValue>(val.Count());
            foreach (IConvertible x in val) { elements.Add(new EncodedValue(x)); }
        }
        public EncodedArray(IEnumerable<EncodedValue> val) : base(default(IEncodable))
        {     
            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0)
            {
                throw new ArgumentException("Array of EncodedValues cannot be empty.");
            }
            else
            {
                elementType = val.First().Type;
                var secondElementType = val.First().ElementType;
                var id = val.First().ID;

                if (val.Any((ev) => ev.ID != id || ev.Type != elementType || ev.ElementType != secondElementType))
                {
                    throw new ArgumentException("An array of encoded values cannot be set unless all values are of the same type.");
                }

                elements = new List<EncodedValue>(val);

                Value = GetArray(val, elementType);
            }
        }
        public EncodedArray(IEnumerable<EncodedArray> val) : base(default(IEncodable))
        {     
            if (val is null) throw new ArgumentNullException("val");
            else if (val.Count() == 0)
            {
                throw new ArgumentException("Array of EncodedValues cannot be empty.");
            }
            else
            {
                elementType = val.First().Type;
                var secondElementType = val.First().ElementType;
                var id = val.First().ID;

                if (val.Any((ev) => ev.ID != id || ev.Type != elementType || ev.ElementType != secondElementType))
                {
                    throw new ArgumentException("An array of encoded values cannot be set unless all values are of the same type.");
                }

                elements = new List<EncodedValue>(val);

                Value = GetArray(val, elementType);
            }
        }

        //PRIVATE CONSTRUCTORS
        private EncodedArray(EncodedValue[] vals, EncodedType type) : base(GetArray(vals, type))
        {
            elementType = type;

            elements = vals.ToList();                 
        }
        private EncodedArray(EncodedValue[] vals, EncodedType type, int id) : base(GetArray(vals, type, id))
        {
            elementType = type;

            elements = vals.ToList();                 
        }

        //PUBLIC FUNCTIONS
        public override int Write(Stream stream)
        {
            if (!stream?.CanWrite ?? true) throw new EncodingException("Stream is null or unable to write to stream.", EncodingExceptionReason.FailedToWriteStream);

            int w;

            try
            {
                w = stream.Write(elementType);
                if (elementType == EncodedType.Object) { w += stream.Write(ID); }
                w += stream.Write(elements.Count);
            }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToWriteStream, e); }

            if (elements.Count > 0)
            {
                foreach (EncodedValue ev in elements) { w += ev.Write(stream); }
            }

            return w;
        }

        //STATIC FUNCTIONS
        public static EncodedArray GetEncodedArray<T>(IEnumerable<T> arr) where T : IEncodable
        {
            DDEncoder.RegisterType(typeof(T));

            if (arr.Count() > 0)
            {
                return new EncodedArray(arr.Cast<IEncodable>(), arr, typeof(T).GetEncoderID());
            }
            else
            {
                return new EncodedArray(new IEncodable[0], arr, typeof(T).GetEncoderID());
            }
        }
        public static EncodedArray GetEncodedArray<T>(T[] arr) where T : IConvertible
        {
            if (arr.Length > 0)
            {
                return new EncodedArray(arr.Cast<IConvertible>(), arr, typeof(T));
            }
            else
            {
                return new EncodedArray(new IConvertible[0], arr, typeof(T));
            }
        }
        public static int Read(Stream stream, out EncodedValue val)
        {
            if (!stream?.CanRead ?? true) throw new EncodingException("Stream is null or unable to read from stream.", EncodingExceptionReason.FailedToReadStream);

            EncodedType type;
            int r, id = 0;
            int len;

            try
            {
                r = stream.ReadEncodedType(out type);
                if (type == EncodedType.Object) { r += stream.ReadInt(out id); }
                r += stream.ReadInt(out len);
            }
            catch (IOException e) { throw new EncodingException(EncodingExceptionReason.FailedToReadStream, e); }

            if (len < 0) throw new EncodingException("Array length less than zero read.", EncodingExceptionReason.BadBinaryHeader);

            EncodedValue[] ret = new EncodedValue[len];

            for (int x = 0; x < ret.Length; x++)
            {
                if (type == EncodedType.Object) { r += EncodedObject.Read(stream, out ret[x]); }
                else { r += Read(stream, type, out ret[x]); }
            }

            if (type == EncodedType.Object) { val = new EncodedArray(ret, type, id); }
            else { val = new EncodedArray(ret, type); }

            return r;
        }
        private static object GetArray(IEnumerable<EncodedValue> vals, EncodedType type, int id = 0)
        {
            Array arr;

            switch (type)
            {
                case EncodedType.Boolean:
                    arr = new bool[vals.Count()];
                    break;
                case EncodedType.Char:
                    arr = new char[vals.Count()];
                    break;
                case EncodedType.SByte:
                    arr = new sbyte[vals.Count()];
                    break;
                case EncodedType.Byte:
                    arr = new byte[vals.Count()];
                    break;
                case EncodedType.Int16:
                    arr = new short[vals.Count()];
                    break;
                case EncodedType.UInt16:
                    arr = new ushort[vals.Count()];
                    break;
                case EncodedType.Int32:
                    arr = new int[vals.Count()];
                    break;
                case EncodedType.UInt32:
                    arr = new uint[vals.Count()];
                    break;
                case EncodedType.Int64:
                    arr = new long[vals.Count()];
                    break;
                case EncodedType.UInt64:
                    arr = new ulong[vals.Count()];
                    break;
                case EncodedType.Single:
                    arr = new float[vals.Count()];
                    break;
                case EncodedType.Double:
                    arr = new double[vals.Count()];
                    break;
                case EncodedType.Decimal:
                    arr = new decimal[vals.Count()];
                    break;
                case EncodedType.DateTime:
                    arr = new DateTime[vals.Count()];
                    break;
                case EncodedType.String:
                    arr = new string[vals.Count()];
                    break;
                case EncodedType.Object:

                    if (id == 0) { throw new EncodingException("ID passed to GetArray must be non-zero if type is Object.", EncodingExceptionReason.BadBinaryHeader); }
                    else
                    {
                        arr = Array.CreateInstance(DDEncoder.GetRegisteredType(id).Type, vals.Count());
                    }

                    break;
                case EncodedType.Empty:                
                case EncodedType.Array:
                default:
                    throw new EncodingException("Cannot create array object from this typecode: " + type, EncodingExceptionReason.BadBinaryHeader);
            }

            if (vals.Count() > 0)
            {
                try
                {
                    for (int x = 0; x < vals.Count(); x++)
                    {
                        arr.SetValue(vals.ElementAt(x).Value, x);
                    }
                }
                catch (Exception e) { throw new EncodingException("Unable to set array value.", EncodingExceptionReason.Other, e); }
            }

            return arr;
        }
    }
}
