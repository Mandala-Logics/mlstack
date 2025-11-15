using System.Collections;
using System.Collections.Generic;
using DDEncoder;

namespace mlEncodedDB
{
    public sealed class BlockListCache
    {
        public int Capacity {get;}

        private readonly Dictionary<int, IEncodable> cache;

        internal BlockListCache(int capacity)
        {
            Capacity = capacity;
            cache = new Dictionary<int, IEncodable>(capacity);
        }

        public bool TrySet(int index, IEncodable val)
        {
            if (cache.ContainsKey(index))
            {
                cache[index] = val;
                return true;
            }
            else if (cache.Count >= Capacity) { return false; }
            else
            {
                cache.Add(index, val);
                return true;
            }
        }

        public bool TryGet(int index, out IEncodable? val)
        {
            if (cache.ContainsKey(index))
            {
                val = cache[index];
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
    }
}