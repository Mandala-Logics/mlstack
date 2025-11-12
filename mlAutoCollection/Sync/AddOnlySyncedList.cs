using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace mlAutoCollection.Sync
{
    public sealed class AddOnlySyncedList<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection
    {
        //PUBLIC PROPERTIES
        public T this[int index] { get => baseList[index]; set => baseList[index] = value; }
        public int Count => baseList.Count;
        public object SyncRoot => ((ICollection)baseList).SyncRoot;
        bool ICollection.IsSynchronized => true;

        //PRIVATE PROPERTIES
        private readonly IList<T> baseList;

        //CONSTRCUTORS
        public AddOnlySyncedList() { baseList = new List<T>(); }
        public AddOnlySyncedList(IList<T> list) { baseList = list ?? throw new ArgumentNullException("list"); }
        public AddOnlySyncedList(int capacity) { baseList = new List<T>(capacity); }
        public AddOnlySyncedList(IEnumerable<T> vals) { baseList = new List<T>(vals); }

        //PUBLIC METHODS
        public void Add(T item) => baseList.Add(item);
        public int IndexOf(T item)
        {
            int x = 0;

            foreach (T val in this)
            {
                if (EqualityComparer<T>.Default.Equals(val, item)) return x;

                x++;
            }

            return -1;
        }
        public bool Contains(T item)
        {
            foreach (T val in this)
            {
                if (EqualityComparer<T>.Default.Equals(val, item)) return true;
            }

            return false;
        }
        public void Insert(int index, T item) => baseList.Insert(index, item);        
        public IEnumerable<T> GetValues()
        {
            lock (SyncRoot)
            {
                T[] arr = new T[baseList.Count];

                baseList.CopyTo(arr, 0);

                return arr;
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                baseList.CopyTo(array, arrayIndex);
            }
        }
        public void CopyTo(Array array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                var arr = baseList.ToArray();

                Array.Copy(baseList.ToArray(), 0, array, arrayIndex, arr.Length);
            }
        }
        public IEnumerator<T> GetEnumerator() => GetValues().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)GetValues()).GetEnumerator();
    }
}
