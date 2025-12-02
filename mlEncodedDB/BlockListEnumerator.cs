using System;
using System.Collections;
using System.Collections.Generic;
using DDEncoder;

namespace mlEncodedDB
{
    public sealed partial class BlockList : IDisposable, IEnumerable<IEncodable>
    {
        public sealed class BlockListEnumerator : IEnumerator<IEncodable>
        {
            public IEncodable Current { get; private set; }
            object IEnumerator.Current => Current;

            private readonly BlockList owner;
            private int pos;
            private int n;
            private List<int> skip;
            private volatile bool haveDecremented = false;

            public BlockListEnumerator(BlockList owner)
            {
                this.owner = owner;

                Reset();
            }

            ~BlockListEnumerator()
            {
                Dispose();
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (!haveDecremented)
                    {
                        owner.enumsOpen--;
                        haveDecremented = true;
                    }
                }
            }

            public bool MoveNext()
            {
                while (pos < owner.btes.Count - 1)
                {
                    pos++;

                    if (skip.Contains(pos))
                    {
                        if (owner.btes[pos].NextBlock > 0) { skip.Add(owner.btes[pos].NextBlock); }

                        continue;
                    }
                    else if (owner.btes[pos].Empty)
                    {
                        continue;
                    }
                    else
                    {
                        if (owner.btes[pos].NextBlock > 0) { skip.Add(owner.btes[pos].NextBlock); }

                        if (owner.cache.TryGet(pos, out IEncodable? x))
                        {
                            Current = x;
                        }
                        else
                        {
                            Current = owner.ReadObject(pos);

                            owner.cache.TrySet(pos, Current);
                        }

                        n++;

                        return true;
                    }
                }

                lock (this)
                {
                    if (!haveDecremented)
                    {
                        owner.enumsOpen--;
                        haveDecremented = true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                pos = -1;
                n = 0;
                skip = new List<int>() { 0 };
            
                lock (owner.SyncRoot)
                {
                    owner.enumsOpen++;
                    haveDecremented = false;
                }
            }
        }
    }
}