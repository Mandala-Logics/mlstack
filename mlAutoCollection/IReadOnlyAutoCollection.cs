using System;
using System.Collections.Generic;
using System.Collections;

namespace mlAutoCollection
{
    public interface ITypedAutoCollection<keyT, valT> : IReadOnlyTypedAutoCollection<keyT, valT>
    {
        void AddRange(IEnumerable<valT> vals);
        bool Remove(keyT key);
        void Add(valT value);
        void Clear();
        void Insert(int index, valT value);
        bool Remove(valT value);
    }

    public interface IReadOnlyTypedAutoCollection<keyT, valT> : IReadOnlyAutoCollection, IReadOnlyDictionary<keyT, valT>
    {
        valT ElementAtKey(keyT key);
        int IndexOfKey(keyT key);
        new int Count {get;}
        valT this[int index] { get; }
        int IndexOf(valT value);
        bool Contains(valT value);
        valT ElementAtKey(object key);
        valT ElementAt(int index);
    }

    public interface IReadOnlyAutoCollection<T> : IEnumerable<T>, IReadOnlyAutoCollection
    {
        T this[int index] { get; }
        T this[object key] { get; }
        IEnumerable<T> GetValues();
        int IndexOf(T value);
        bool Contains(T value);
        T ElementAtKey(object key);
        T ElementAt(int index);
    }
    
    public interface IReadOnlyAutoCollection : ICollection, IEnumerable
    {
        event CollectionCancelEventHandler BeforeAdd;
        event CollectionCancelEventHandler BeforeRemove;
        event CollectionEventHandler AfterAdd;
        event CollectionEventHandler AfterRemove;
        event CollectionEventHandler KeyChanged;
        event EventHandler Cleared;

        ICollectable GetValue(object key);
        object GetValue(int index);
        object GetKey(int index);
        int IndexOfKey(object key);
        int IndexOf(ICollectable value);
        bool ContainsKey(object key);
        IEnumerable<object> GetKeys();
        IEqualityComparer KeyComparaer { get; }
    }
}
