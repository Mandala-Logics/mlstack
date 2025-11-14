using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace mlAutoCollection.Sync
{
    public sealed class SyncedEnumerator<T> : IEnumerator<T>
    {
        public T Current {get; private set;}

        object IEnumerator.Current => Current;
        public object SyncRoot => ((ICollection)baseList).SyncRoot;

        private readonly IList<T> baseList;
        private int pos = -1;

        internal SyncedEnumerator(IList<T> baseList)
        {
            this.baseList = baseList;
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            lock (SyncRoot)
            {
                pos++;

                if (pos < baseList.Count)
                {
                    Current = baseList[pos];
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        public void Reset()
        {
            pos = -1;
        }
    }
}