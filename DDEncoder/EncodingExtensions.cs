using System.IO;
using System;
using System.Linq;
using System.Threading;
using mlThreadMGMT;

namespace DDEncoder
{
    public static class EncodingExtensions
    {
        //WRITE EXTENSIONS
        public static int Write(this Stream stream, params EncodedType[] types)
        {
            if (!stream?.CanWrite ?? true) throw new EncodingException("Stream is null or unable to write to stream.", EncodingExceptionReason.FailedToWriteStream);

            var b = types.Cast<byte>().ToArray();

            stream.Write(b, 0, b.Length);

            return b.Length;
        }
        public static int Write(this Stream stream, int i)
        {
            if (!stream?.CanWrite ?? true) throw new IOException("Stream is null or unable to write to stream.");

            var b = BitConverter.GetBytes(i);

            stream.Write(b, 0, b.Length);

            return b.Length;
        }

        //READ EXTENSIONS
        public static int ReadEncodedType(this Stream stream, out EncodedType et)
        {
            if (!stream?.CanRead ?? true) throw new EncodingException("Stream is null or unable to read from stream.", EncodingExceptionReason.FailedToReadStream);

            int b = stream.ReadByte();

            if (b == -1) throw new EncodingException("Failed to read from stream: end of stream?", EncodingExceptionReason.FailedToReadStream);
            else
            {
                if (!Enum.IsDefined(typeof(EncodedType), (byte)b)) 
                {
                    throw new EncodingException("Unrecognised type code read from stream.", EncodingExceptionReason.BadBinaryHeader);
                } 
                else 
                {
                    et = (EncodedType)b;
                    return sizeof(byte); 
                }
            }
        }
        public static int ReadInt(this Stream stream, out int i)
        {
            if (!stream?.CanRead ?? true) throw new IOException("Stream is null or unable to read from stream.");

            var buffer = new byte[sizeof(int)];

            int x = stream.Read(buffer, 0, sizeof(int));

            if (x < sizeof(int)) throw new IOException("Not enough bytes read.");

            i = BitConverter.ToInt32(buffer, 0);

            return sizeof(int);
        }
        public static WaitBase CopyToWait(this Stream stream, Stream dst, IProgress<long> progress = default, CancellationToken cancellationToken = default)
        {
            var tt = new ThreadTask((tc) => DoCopy(tc, stream, dst), progress, cancellationToken);
            tt.Start();

            return tt.Waiter;
        }
        private static void DoCopy(ThreadController tc, Stream src, Stream dst)
        {   
            byte[] buffer = new byte[Math.Min(1024 * 100, src.Length)];

            int r;
            long x = 0L;

            do
            {
                tc.ThrowIfAborted();

                r = src.Read(buffer, 0, buffer.Length);

                if (r > 0)
                {
                    dst.Write(buffer, 0, r);

                    tc.Progress.Report(x += r);
                }                

            } while (r > 0);
        }

        //TYPE EXTENSIONS
        public static EncodedType GetEncodedType(this Type type)
        {
            if (type is null) throw new ArgumentNullException("type");

            if (type.IsArray) { return EncodedType.Array; }
            else { return (EncodedType)Type.GetTypeCode(type); }
        }
        public static int GetEncoderID(this Type type) => DDHash.HashString32(type.FullName);

        //IENCODABLE EXTENSIONS
        public static int GetEncoderID(this IEncodable obj) => DDHash.HashString32(obj.GetType().FullName);
        public static bool IsFixedSize(this IEncodable obj)
        {
            try { return EncodedObject.GetEncodedObject(obj).IsFixedSize; }
            catch (Exception e) { throw new Exception("Failed to get size of an IEncodable object", e); }
        }
        public static int GetEncodedSize(this IEncodable obj)
        {
            try { return EncodedObject.GetEncodedObject(obj).EncodedSize + 1; }
            catch (Exception e) { throw new Exception("Failed to get size of an IEncodable object", e); }
        }
        public static IEncodable EncoderCopy(this IEncodable obj)
        {
            MemoryStream ms = null;

            try 
            {
                var eo = EncodedObject.GetEncodedObject(obj);

                ms = new MemoryStream();

                eo.Write(ms);
                ms.Position = 0;

                EncodedObject.Read(ms, out EncodedValue ev);

                eo = (EncodedObject)ev;

                return (IEncodable)eo.Value;

            }
            catch (EncodingException e) { throw new Exception("Failed to copy IEncodable object.", e); }
            finally { ms?.Dispose(); }
        }
        public static EncodedObject GetEncodedObject(this IEncodable obj)
        {
            EncodedObject ret = new EncodedObject(obj);

            obj.Encode(ref ret);

            return ret;
        }
    }
}
