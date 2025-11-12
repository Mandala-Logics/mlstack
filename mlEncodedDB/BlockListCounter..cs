using System;
using System.Collections.Generic;
using DDEncoder;

namespace mlEncodedDB
{
    public sealed partial class BlockList : IDisposable, IEnumerable<IEncodable>
    {
        internal sealed class BlockListCounter
        {
            private int? count;
            private readonly BlockList owner;

            internal BlockListCounter(BlockList owner)
            {
                this.owner = owner;
            }

            public void Incrament(int delta)
            {
                if (count is int) { count += delta; }
            }

            public void Decrament(int delta)
            {
                if (count is int)
                {
                    count -= delta;

                    if (count < 0) { throw new InvalidOperationException("Count cannot be reduced below zero."); }
                }
            }

            public int GetCount()
            {
                if (count is int y) { return y; }
                else
                {
                    var skip = new List<int>() { 0 };
                    int c = 0;

                    for (int x = 0; x < owner.btes.Count; x++)
                    {
                        if (skip.Contains(x))
                        {
                            if (owner.btes[x].NextBlock > 0) { skip.Add(owner.btes[x].NextBlock); }
                        }
                        else if (!owner.btes[x].Empty)
                        {
                            if (owner.btes[x].NextBlock > 0) { skip.Add(owner.btes[x].NextBlock); }

                            c++;
                        }
                    }

                    count = c;

                    return c;
                }
            }

            public void Clear()
            {
                count = null;
            }
        }
    }
}