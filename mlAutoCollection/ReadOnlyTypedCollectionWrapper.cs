using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace mlAutoCollection
{
    internal sealed class ReadOnlyTypedCollectionWrapper<keyT, valT> : IReadOnlyTypedAutoCollection<keyT, valT> where valT : ICollectable
    {
        //EVENTS
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeAdd { add => baseCollection.BeforeAdd += value; remove => baseCollection.BeforeAdd -= value; }
        event CollectionCancelEventHandler IReadOnlyAutoCollection.BeforeRemove { add => baseCollection.BeforeRemove += value; remove => baseCollection.BeforeRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterAdd { add => baseCollection.AfterAdd += value; remove => baseCollection.AfterAdd -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.AfterRemove { add => baseCollection.AfterRemove += value; remove => baseCollection.AfterRemove -= value; }
        event CollectionEventHandler IReadOnlyAutoCollection.KeyChanged { add => baseCollection.KeyChanged += value; remove => baseCollection.KeyChanged -= value; }
        event EventHandler IReadOnlyAutoCollection.Cleared { add => baseCollection.Cleared += value; remove => baseCollection.Cleared -= value; }

        //PUBLIC PROPERTIES
        public valT this[keyT key] => baseCollection[key];
        public valT this[object key] => baseCollection[key];
        public valT this[int index] => baseCollection[index];
        public bool IsReadOnly => true;
        public IEqualityComparer KeyComparaer => ((IReadOnlyAutoCollection)baseCollection).KeyComparaer;
        public int Count => baseCollection.Count;
        public bool IsSynchronized => baseCollection.IsSynchronized;
        public object SyncRoot => baseCollection.SyncRoot;
        IEnumerable<keyT> IReadOnlyDictionary<keyT, valT>.Keys => throw new NotImplementedException();
        IEnumerable<valT> IReadOnlyDictionary<keyT, valT>.Values => throw new NotImplementedException();

        //PRIVATE PROPERTIES
        readonly AutoCollection<valT> baseCollection;

        //CONSTRUCTORS
        internal ReadOnlyTypedCollectionWrapper(AutoCollection<valT> coll) { baseCollection = coll; }

        //PUBLIC METHODS
        public bool Contains(valT value) => baseCollection.Contains(value);
        public bool ContainsKey(keyT key) => baseCollection.ContainsKey(key);
        public valT ElementAt(int index) => baseCollection.ElementAt(index);
        public valT ElementAtKey(keyT key) => baseCollection.ElementAtKey(key);        
        public IEnumerator<valT> GetEnumerator() => ((IEnumerable<valT>)baseCollection).GetEnumerator();
        public IEnumerable<keyT> GetKeys() => baseCollection.GetKeys().Cast<keyT>();
        public IEnumerable<valT> GetValues() => baseCollection.GetValues();
        public int IndexOf(valT value) => baseCollection.IndexOf(value);
        public int IndexOfKey(keyT key) => baseCollection.IndexOfKey(key);

        //INTERFACE METHODS
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)baseCollection).GetEnumerator();
        int IReadOnlyAutoCollection.IndexOfKey(object key) => baseCollection.IndexOfKey(key);
        IEnumerable<object> IReadOnlyAutoCollection.GetKeys() => baseCollection.GetKeys();
        bool IReadOnlyAutoCollection.ContainsKey(object key) => baseCollection.ContainsKey(key);
        ICollectable IReadOnlyAutoCollection.GetValue(object key) => baseCollection[key];
        object IReadOnlyAutoCollection.GetValue(int index) => baseCollection[index];
        object IReadOnlyAutoCollection.GetKey(int index) => baseCollection[index].Key;
        void ICollection.CopyTo(Array array, int index) => ((ICollection)baseCollection).CopyTo(array, index);
        int IReadOnlyAutoCollection.IndexOf(ICollectable value)
        {
            if (value is valT val) { return baseCollection.IndexOf(val); }
            else { return -1; }
        }
        bool IReadOnlyDictionary<keyT, valT>.TryGetValue(keyT key, out valT value)
        {
            try
            {
                value = baseCollection.ElementAtKey(key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default;
                return false;
            }
        }
        public valT ElementAtKey(object key) => baseCollection.ElementAtKey(key);
        IEnumerator<KeyValuePair<keyT, valT>> IEnumerable<KeyValuePair<keyT, valT>>.GetEnumerator()
            => new TypedCollectionWrapper<keyT, valT>.TypedCollectionEnumerator(((IEnumerable<valT>)baseCollection).GetEnumerator());        
    }
}
