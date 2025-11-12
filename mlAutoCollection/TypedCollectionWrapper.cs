using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mlAutoCollection
{
    internal sealed class TypedCollectionWrapper<keyT, valT> : ITypedAutoCollection<keyT, valT>, IList where valT : ICollectable
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
        public valT this[int index]
        {
            get => baseCollection [index];
            set
            {
                baseCollection[index] = value;
            }
        }
        public bool IsReadOnly => false;
        public IEqualityComparer KeyComparaer => ((IReadOnlyAutoCollection)baseCollection).KeyComparaer;
        public int Count => baseCollection.Count;
        public bool IsSynchronized => baseCollection.IsSynchronized;
        public object SyncRoot => baseCollection.SyncRoot;
        bool IList.IsFixedSize => false;
        IEqualityComparer IReadOnlyAutoCollection.KeyComparaer => KeyComparaer;
        IEnumerable<keyT> IReadOnlyDictionary<keyT, valT>.Keys => baseCollection.GetKeys().Cast<keyT>();
        IEnumerable<valT> IReadOnlyDictionary<keyT, valT>.Values => baseCollection.GetValues();

        object IList.this[int index]
        {
            get => ElementAt(index);
            set => ((IList)baseCollection)[index] = value;
        }

        //PRIVATE PROPERTIES
        private AutoCollection<valT> baseCollection;        

        //CONSTRUCTORS
        internal TypedCollectionWrapper(AutoCollection<valT> coll) 
        {
            baseCollection = coll;            
        }

        //PUBLIC METHODS
        public void RemoveAt(int index) => baseCollection.RemoveAt(index);
        public void Add(valT value) => baseCollection.Add(value);
        public void AddRange(IEnumerable<valT> vals) => baseCollection.AddRange(vals);
        public bool Contains(valT value) => baseCollection.Contains(value);
        public bool ContainsKey(keyT key) => baseCollection.ContainsKey(key);
        public valT ElementAt(int index) => baseCollection.ElementAt(index);
        public valT ElementAtKey(keyT key) => baseCollection.ElementAtKey(key);
        public IEnumerator<valT> GetEnumerator() => ((IEnumerable<valT>)baseCollection).GetEnumerator();
        public IEnumerable<keyT> GetKeys() => baseCollection.GetKeys().Cast<keyT>();
        public IEnumerable<valT> GetValues() => baseCollection.GetValues();
        public int IndexOf(valT value) => baseCollection.IndexOf(value);
        public int IndexOfKey(keyT key) => baseCollection.IndexOfKey(key);
        public void Insert(int index, valT value) => baseCollection.Insert(index, value);
        public bool Remove(valT value) => baseCollection.Remove(value);
        public bool Remove(keyT key) => baseCollection.Remove(key);
        public void Clear() => baseCollection.Clear();
        public void CopyTo(valT[] array, int index) => baseCollection.CopyTo(array, index);
        public IReadOnlyTypedAutoCollection<keyT, valT> AsReadOnly() => baseCollection.AsTypedReadOnly<keyT>();

        //INTERFACE METHODS
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)baseCollection).GetEnumerator();        
        public valT ElementAtKey(object key) => baseCollection[key];
        int IReadOnlyAutoCollection.IndexOfKey(object key) => baseCollection.IndexOfKey(key);
        IEnumerable<object> IReadOnlyAutoCollection.GetKeys() => baseCollection.GetKeys();
        bool IReadOnlyAutoCollection.ContainsKey(object key) => baseCollection.ContainsKey(key);
        ICollectable IReadOnlyAutoCollection.GetValue(object key) => (ICollectable)baseCollection[key];
        object IReadOnlyAutoCollection.GetValue(int index) => baseCollection[index];
        object IReadOnlyAutoCollection.GetKey(int index) => ((ICollectable)baseCollection[index]).Key;
        int IList.IndexOf(object value)
        {
            if (value is valT val) { return baseCollection.IndexOf(val); }
            else { return -1; }
        }
        int IReadOnlyAutoCollection.IndexOf(ICollectable value)
        {
            if (value is valT val) { return baseCollection.IndexOf(val); }
            else { return -1; }
        }
        void ICollection.CopyTo(Array array, int index) => ((ICollection)baseCollection).CopyTo(array, index);
        int IList.Add(object value) => ((IList)baseCollection).Add(value);
        bool IList.Contains(object value)
        {
            if (value is valT val) { return baseCollection.Contains(val); }
            else { return false; }
        }
        void IList.Insert(int index, object value)
        {
            if (value is valT val)
            {
                baseCollection.Insert(index, val);
            }
            else throw new ArgumentException($"Only values of type {typeof(valT)} can be added to this list.");
        }
        void IList.Remove(object value)
        {
            if (value is valT val)
            {
                baseCollection.Remove(val);
            }
            else throw new ArgumentException($"Value not found.");
        }
        bool IReadOnlyDictionary<keyT, valT>.TryGetValue(keyT key, out valT value)
        {
            try
            {
                value = baseCollection[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default;
                return false;
            }
        }
        IEnumerator<KeyValuePair<keyT, valT>> IEnumerable<KeyValuePair<keyT, valT>>.GetEnumerator()
            => new TypedCollectionEnumerator(((IEnumerable<valT>)baseCollection).GetEnumerator());

        //SUB CLASSES
        internal sealed class TypedCollectionEnumerator : IEnumerator<KeyValuePair<keyT, valT>>
        {
            //PUBLIC PROPERTIES
            public KeyValuePair<keyT, valT> Current => new KeyValuePair<keyT, valT>((keyT)baseEnum.Current.Key, baseEnum.Current);
            object IEnumerator.Current => Current;

            //PRIVATE PROPERTIES
            private readonly IEnumerator<valT> baseEnum;

            //CONSTRCUTORS
            public TypedCollectionEnumerator(IEnumerator<valT> baseEnum)
            {
                this.baseEnum = baseEnum ?? throw new ArgumentNullException(nameof(baseEnum));
            }

            //PRIVATE METHODS
            public void Dispose() => baseEnum.Dispose();
            public bool MoveNext() => baseEnum.MoveNext();
            public void Reset() => baseEnum.Reset();
        }
    }
}
