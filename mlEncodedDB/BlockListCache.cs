using System.Collections;
using System.Collections.Generic;
using DDEncoder;

namespace mlEncodedDB
{
    internal sealed class BlockListCache
    {
        public int Capacity {get;}

        private readonly Dictionary<int, IEncodable> cache;

        internal BlockListCache(int capacity)
        {
            Capacity = capacity;
            cache = new Dictionary<int, IEncodable>(capacity);
        }

        public bool TrySet(int bteIndex, IEncodable val)
        {
            if (cache.ContainsKey(bteIndex))
            {
                cache[bteIndex] = val;
                return true;
            }
            else if (cache.Count >= Capacity) { return false; }
            else
            {
                cache.Add(bteIndex, val);
                return true;
            }
        }

        public bool TryGet(int bteIndex, out IEncodable? val)
        {
            if (cache.ContainsKey(bteIndex))
            {
                val = cache[bteIndex];
                return true;
            }
            else
            {
                val = null;
                return false;
            }
        }

        public void Clear()
        {
            cache.Clear();
        }

        public void Remove(int bteIndex)
        {
            cache.Remove(bteIndex);
        }
    }
}