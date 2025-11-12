using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDEncoder
{
    public sealed class EncodedValueCollection : IDictionary<string, EncodedValue>, IEncodable, ICloneable
    {
        //PRIVATE PROPERTIES
        private readonly ConcurrentDictionary<string, EncodedValue> vals;

        //PUBLIC PROPERTIES
        public EncodedValue this[int key] => vals.ElementAt(key).Value;
        public IEqualityComparer<string> Comparer;
        public StringComparison Comparison { get; }
        public IEnumerable<string> Keys => vals.Keys;
        public IEnumerable<EncodedValue> Values => vals.Values;
        public int Count => vals.Count;
        ICollection<string> IDictionary<string, EncodedValue>.Keys => vals.Keys.ToList();
        ICollection<EncodedValue> IDictionary<string, EncodedValue>.Values => vals.Values.ToList();
        public bool IsReadOnly => false;
        public EncodedValue this[string key] { get => vals[key]; set => vals[key] = value; }

        //CONSTRUCTORS
        static EncodedValueCollection()
        {
            DDEncoder.RegisterType(typeof(EncodedValueCollection));
        }
        public EncodedValueCollection() : this(0, StringComparison.Ordinal) { }
        public EncodedValueCollection(int capacity, StringComparison stringComparison)
        {
            if (capacity < 0) { throw new ArgumentException($"Capacity must be greater than or equal to zero. Invalid value: {capacity}"); }

            Comparison = stringComparison;

            switch (Comparison)
            {
                case StringComparison.CurrentCulture:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.CurrentCulture);
                    break;
                case StringComparison.CurrentCultureIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.CurrentCultureIgnoreCase);
                    break;
                case StringComparison.InvariantCulture:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.InvariantCulture);
                    break;
                case StringComparison.InvariantCultureIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.InvariantCultureIgnoreCase);
                    break;
                case StringComparison.Ordinal:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.Ordinal);
                    break;
                case StringComparison.OrdinalIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.OrdinalIgnoreCase);
                    break;
                default:
                    throw new ArgumentException($"stringComparison {stringComparison} is not valid.");
            }
        }
        public EncodedValueCollection(int capacity) : this(capacity, StringComparison.Ordinal) { }
        public EncodedValueCollection(StringComparison stringComparison) : this(0, stringComparison) { }

        //PUBLIC METHODS
        public bool ContainsKey(string key)
        {
            return vals.ContainsKey(key);
        }
        public IEnumerator<KeyValuePair<string, EncodedValue>> GetEnumerator()
        {
            return vals.GetEnumerator();
        }
        public bool TryGetValue(string key, out EncodedValue value)
        {
            return vals.TryGetValue(key, out value);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)vals).GetEnumerator();
        }

        //ENCODING
        public EncodedValueCollection(EncodedObject eo)
        {
            var c = eo.Next<int>();
            Comparison = eo.Next<StringComparison>();

            switch (Comparison)
            {
                case StringComparison.CurrentCulture:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.CurrentCulture);
                    break;
                case StringComparison.CurrentCultureIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.CurrentCultureIgnoreCase);
                    break;
                case StringComparison.InvariantCulture:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.InvariantCulture);
                    break;
                case StringComparison.InvariantCultureIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.InvariantCultureIgnoreCase);
                    break;
                case StringComparison.Ordinal:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.Ordinal);
                    break;
                case StringComparison.OrdinalIgnoreCase:
                    vals = new ConcurrentDictionary<string, EncodedValue>(Comparer = StringComparer.OrdinalIgnoreCase);
                    break;
            }

            for (int x = 0; x < c; x++)
            {
                vals.TryAdd(eo.Next<string>(), eo.Next());
            }
        }
        public void Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(Count);
            encodedObj.Append(Comparison);

            foreach (var kvp in vals)
            {
                encodedObj.Append(kvp.Key);
                encodedObj.Append(kvp.Value);
            }
        }
        public void Add(string key, EncodedValue value)
        {
            vals.TryAdd(key, value);
        }
        public bool Remove(string key)
        {
            return vals.TryRemove(key, out _);
        }
        public void Add(KeyValuePair<string, EncodedValue> item)
        {
            vals.TryAdd(item.Key, item.Value);
        }
        public void Clear()
        {
            vals.Clear();
        }
        public bool Contains(KeyValuePair<string, EncodedValue> item)
        {
            return vals.Contains(item);
        }
        public void CopyTo(KeyValuePair<string, EncodedValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, EncodedValue>>)vals.ToArray()).CopyTo(array, arrayIndex);
        }
        public bool Remove(KeyValuePair<string, EncodedValue> item)
        {
            return vals.TryRemove(item.Key, out _);
        }

        public object Clone()
        {
            var clone = new EncodedValueCollection(Comparison);
            foreach (var kvp in vals)
            {
                clone.Add(kvp.Key, kvp.Value);
            }
            return clone;
        }
    }

    public sealed class ObjectValueCollection : IReadOnlyDictionary<string, object>, IEncodable, ICloneable
    {
        //PUBLIC PROPERTIES
        public EncodedValueCollection BaseCollection { get; }
        public IEnumerable<string> Keys => (ICollection<string>)BaseCollection.Keys;
        public IEnumerable<object> Values
        {
            get
            {
                var ret = new List<object>(BaseCollection.Count);

                foreach (var kvp in BaseCollection)
                {
                    ret.Add(kvp.Value.Value);
                }

                return ret;
            }
        }
        public int Count => BaseCollection.Count;
        public bool IsReadOnly => BaseCollection.IsReadOnly;
        public object this[string key]
        {
            get => BaseCollection[key].Value;
            set
            {
                if (DDEncoder.TryCastToEncoded(value, out EncodedValue ev))
                {
                    BaseCollection[key] = ev;
                }
                else
                {
                    throw new InvalidCastException("Objects passed to this collection need to implement ICollectable or IEncodable, or be IEnumerables thereof.");
                }
            }
        }

        //CONSTRUCTORS
        public ObjectValueCollection(EncodedValueCollection baseCollection)
        {
            BaseCollection = baseCollection ?? throw new ArgumentNullException(nameof(baseCollection));
        }
        public ObjectValueCollection(int capacity, StringComparison stringComparison)
        {
            BaseCollection = new EncodedValueCollection(capacity, stringComparison);
        }
        static ObjectValueCollection()
        {
            DDEncoder.RegisterTypes(typeof(ObjectValueCollection), typeof(EncodedValueCollection));
        }
        public ObjectValueCollection(EncodedObject eo)
        {
            BaseCollection = eo.Next<EncodedValueCollection>();
        }

        //ENCODING
        public void Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(BaseCollection);
        }

        //PUBLIC METHODS
        public void Add(string key, IConvertible value)
        {
            if (!DDEncoder.TryCastToEncoded(value, out EncodedValue ev))
            {
                throw new InvalidCastException("Objects passed to this collection need to implement ICollectable or IEncodable, or be IEnumerables thereof.");
            }

            BaseCollection.Add(key, ev);
        }
        public void Add(string key, IEncodable value)
        {
            if (!DDEncoder.TryCastToEncoded(value, out EncodedValue ev))
            {
                throw new InvalidCastException("Objects passed to this collection need to implement ICollectable or IEncodable, or be IEnumerables thereof.");
            }

            BaseCollection.Add(key, ev);
        }
        public void Add<T>(string key, IEnumerable<T> value) where T : IEncodable
        {
            BaseCollection.Add(key, EncodedArray.GetEncodedArray(value));
        }
        public void Add<T>(string key, T[] value) where T : IConvertible
        {
            BaseCollection.Add(key, EncodedArray.GetEncodedArray(value));
        }
        public bool ContainsKey(string key) => BaseCollection.ContainsKey(key);
        public bool Remove(string key) => BaseCollection.Remove(key);
        public bool TryGetValue(string key, out object value)
        {
            if (BaseCollection.TryGetValue(key, out var ev))
            {
                value = ev.Value;
                return true;
            }
            value = null;
            return false;
        }
        public void Clear() => BaseCollection.Clear();
        public bool Contains(KeyValuePair<string, object> item)
        {
            return BaseCollection.TryGetValue(item.Key, out var ev) && ev.Value.Equals(item.Value);
        }
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            int x = arrayIndex - 1;

            foreach (var kvp in BaseCollection)
            {
                if (x++ >= array.Length) { return; }

                array[x] = new KeyValuePair<string, object>(kvp.Key, kvp.Value.Value);
            }
        }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => new ObjectValueCollectionEnumerator(BaseCollection);
        IEnumerator IEnumerable.GetEnumerator() => new ObjectValueCollectionEnumerator(BaseCollection);

        public object Clone()
        {
            return new ObjectValueCollection((EncodedValueCollection)BaseCollection.Clone());
        }
    }

    public sealed class ObjectValueCollectionEnumerator : IEnumerator<KeyValuePair<string, object>>
    {
        //PUBLIC PROPERTIES
        public KeyValuePair<string, object> Current {get;private set;}
        object IEnumerator.Current => Current;

        //PRIVATE PROPERTIES
        private readonly EncodedValueCollection coll;
        private int x = -1;

        //CONSTRUCTORS
        public ObjectValueCollectionEnumerator(EncodedValueCollection collection)
        {
            coll = collection;
        }

        //PUBLIC METHODS
        public void Dispose() { }
        public bool MoveNext()
        {
            if (x++ == coll.Count) { return false; }

            Current = new KeyValuePair<string, object>(coll.ElementAt(x).Key, coll.ElementAt(x).Value.Value);

            return true;
        }
        public void Reset()
        {
            x = -1;
        }
    }
}