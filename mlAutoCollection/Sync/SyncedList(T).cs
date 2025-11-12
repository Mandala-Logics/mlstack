using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mlAutoCollection.Sync
{
    public sealed class SyncedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection
    {
        // PUBLIC PROPERTIES
        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return baseList.Count;
                }
            }
        }
        public object SyncRoot => ((ICollection)baseList).SyncRoot;
        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return baseList[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    baseList[index] = value;
                }
            }
        }
        public bool IsReadOnly => false;

        // INTERFACE PROPERTIES              
        bool ICollection.IsSynchronized => true;

        // PRIVATE FIELDS
        private readonly IList<T> baseList;

        // CONSTRUCTORS
        public SyncedList(IList<T> list)
        {
            baseList = list ?? throw new ArgumentNullException(nameof(list));
        }
        public SyncedList()
        {
            baseList = new List<T>();
        }
        public SyncedList(int capacity)
        {
            baseList = new List<T>(capacity);
        }
        public SyncedList(IEnumerable<T> vals)
        {
            baseList = vals.ToList();
        }

        // PUBLIC METHODS
        public void Sort()
        {
            Sort(Comparer<T>.Default);
        }

        public void Sort(IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                var list = baseList as List<T>;
                list?.Sort(comparer);
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return baseList.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                baseList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                baseList.RemoveAt(index);
            }
        }

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                baseList.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                baseList.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return baseList.Contains(item);
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
                ((ICollection)baseList).CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return baseList.Remove(item);
            }
        }

        public IEnumerable<T> GetValues()
        {
            lock (SyncRoot)
            {
                return baseList.ToList();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetValues().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetValues().GetEnumerator();
        }
    }
}
