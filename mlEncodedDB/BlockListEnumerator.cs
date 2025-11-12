using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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
            private List<int> skip;

            public BlockListEnumerator(BlockList owner)
            {
                this.owner = owner;

                Reset();
            }

            public void Dispose() { }

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

                        Current = owner.ReadObject(pos);

                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                pos = -1;
                skip = new List<int>() { 0 };
            }
        }
    }
}