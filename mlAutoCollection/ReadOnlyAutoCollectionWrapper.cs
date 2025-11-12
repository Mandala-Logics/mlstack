using System;
using System.Collections;
using System.Collections.Generic;

namespace mlAutoCollection
{
    internal sealed class ReadOnlyAutoCollectionWrapper<T> : IReadOnlyAutoCollection<T>
    {
        //EVENTS
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeAdd { add => baseCollection.BeforeAdd += value; remove => baseCollection.BeforeAdd -= value; }
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeRemove { add => baseCollection.BeforeRemove += value; remove => baseCollection.BeforeRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterAdd { add => baseCollection.AfterAdd += value; remove => baseCollection.AfterAdd -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterRemove { add => baseCollection.AfterRemove += value; remove => baseCollection.AfterRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.KeyChanged { add => baseCollection.KeyChanged += value; remove => baseCollection.KeyChanged -= value; }
        event EventHandler IReadOnlyAutoCollection.Cleared { add => baseCollection.Cleared += value; remove => baseCollection.Cleared -= value; }

        //PUBLIC PROPERTIES
        public T this[object key] => baseCollection[key];
        public T this[int index] => baseCollection[index];
        public bool IsReadOnly => true;
        public int Count => baseCollection.Count;
        IEqualityComparer IReadOnlyAutoCollection.KeyComparaer => baseCollection.KeyComparaer;
        public bool IsSynchronized => baseCollection.IsSynchronized;
        public object SyncRoot => baseCollection.SyncRoot;

        //PRIVATE FIELDS
        private IReadOnlyAutoCollection<T> baseCollection;

        //CONSTRUCTORS
        internal ReadOnlyAutoCollectionWrapper(IReadOnlyAutoCollection<T> coll) { baseCollection = coll; }

        //PUBLIC FUCNTIONS
        public bool Contains(T value) => baseCollection.Contains(value);
        public bool ContainsKey(object key) => baseCollection.ContainsKey(key);
        public T ElementAt(int index) => baseCollection.ElementAt(index);
        public T ElementAtKey(object key) => baseCollection.ElementAtKey(key);
        public int IndexOf(T value) => baseCollection.IndexOf(value);
        public int IndexOfKey(object key) => baseCollection.IndexOfKey(key);
        public IEnumerable<object> GetKeys() => baseCollection.GetKeys();
        public IEnumerable<T> GetValues() => baseCollection.GetValues();

        //INTERFACE FUNCTIONS
        public IEnumerator<T> GetEnumerator() => baseCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => baseCollection.GetEnumerator();
        ICollectable IReadOnlyAutoCollection.GetValue(object key) => (ICollectable)baseCollection[key];
        object IReadOnlyAutoCollection.GetValue(int index) => baseCollection[index];
        object IReadOnlyAutoCollection.GetKey(int index) => ((ICollectable)baseCollection[index]).Key;
        void ICollection.CopyTo(Array array, int index) => ((ICollection)baseCollection).CopyTo(array, index);
        int IReadOnlyAutoCollection.IndexOf(ICollectable value)
        {
            if (value is T val) { return baseCollection.IndexOf(val); }
            else { return -1; }
        }
    }
}
