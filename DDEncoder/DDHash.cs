using System;
using System.Collections;
using System.Text;

namespace DDEncoder
{
    public static class DDHash
    {
        public static char[] HexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        //PUBLIC PROPERTIES
        public static readonly Random Rand = new Random();
        public static int RandomHash => Rand.Next();

        //PRIVATE PROPERTIES
        private static readonly long p64 = 31L;
        private static readonly long m64 = 1000000007L;
        private static readonly int p32 = 31;
        private static readonly int m32 = 1000000007;
        public static readonly byte ZeroByte = 0;

        //STATIC METHODS
        public static void Shuffle<T>(T[] arr, int seed)
        {
            Random rng = new Random(seed);

            int n = arr.Length;

            while (n > 1)
            {
                int k = rng.Next(n--);
                (arr[k], arr[n]) = (arr[n], arr[k]);
            }
        }
        public static void Shuffle<T>(T[] arr)
        {
            Random rng = new Random(); 

            int n = arr.Length;

            while (n > 1)
            {
                int k = rng.Next(n--);
                (arr[k], arr[n]) = (arr[n], arr[k]);
            }
        }
        public static byte[] GetRandomBytes(int length)
        {
            var buffer = new byte[length];

            Rand.NextBytes(buffer);

            return buffer;
        }
        public static string GetRandomHexString(int length)
        {
            if (length < 0) { throw new ArgumentException("length cannot be less than zero"); }
            else if (length == 0) { return string.Empty; }

            var sb = new StringBuilder(length);

            for (int x = 1; x <= length; x++)
            {
                var rand = Math.Abs(Rand.Next());

                var index = rand % HexChars.Length;

                sb.Append(HexChars[index]);
            }

            return sb.ToString();
        }
        public static long HashBytes(byte[] bytes, int length)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentNullException(nameof(bytes));

            if (length <= 0 || length > bytes.Length)
                throw new ArgumentException("Length cannot be less than zero or greater than the byte count.");
            
            unchecked
            {
                int hash = 17;

                for (int c = 0; c < length; c++) { hash = hash * 23 + bytes[c]; }

                return hash;
            }
        }
        public static long HashString64(string s)
        {
            long hash_so_far = 0;
            long p_pow = 1;

            unchecked
            {
                foreach (char c in s)
                {
                    hash_so_far = (hash_so_far + (c - 'b') * p_pow) % m64;
                    p_pow = p_pow * p64 % m64;
                }
            }

            return hash_so_far;
        }
        public static int HashString32(string s)
        {
            int hash_so_far = 0;
            int p_pow = 1;

            unchecked
            {
                foreach (char c in s)
                {
                    hash_so_far = (hash_so_far + (c - 'b') * p_pow) % m32;
                    p_pow = p_pow * p32 % m32;
                }
            }

            return hash_so_far;
        }
        public static bool IsBlank(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                if (!b.Equals(ZeroByte))
                {
                    return false;
                }
            }

            return true;
        }
    }
}