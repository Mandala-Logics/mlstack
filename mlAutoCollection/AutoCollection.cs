using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using mlAutoCollection.Sync;

namespace mlAutoCollection
{
    public partial class AutoCollection<T> : IReadOnlyAutoCollection<T>, IList, IList<T> where T : ICollectable
    {
        //EVENTS
        public event CollectionCancelEventHandler BeforeAdd;
        public event CollectionCancelEventHandler BeforeRemove;
        public event CollectionEventHandler AfterAdd;
        public event CollectionEventHandler AfterRemove;
        public event CollectionEventHandler KeyChanged;
        public event EventHandler Cleared;

        //PUBLIC PROPERTIES
        public T this[int index]
        {
            get
            {
                //Debug.WriteLine(index);
                return BaseList[index];
            }
            set
            {
                lock (SyncRoot) { BaseList[index] = value; }
            }
        }
        public T this[object key] => ElementAtKey(key);
        public virtual bool IsReadOnly => false;        
        public bool IsSynchronized { get; }
        public object SyncRoot => ((ICollection)BaseList).SyncRoot;
        public int Count => BaseList.Count;
        public bool IgnoreCancel { get; set; } = false;

        //PROTECTED FIELDS
        internal readonly IEqualityComparer keyCompararer;
        protected readonly IList<T> BaseList;        

        //INTERFACE PROPERTIES
        IEqualityComparer IReadOnlyAutoCollection.KeyComparaer => keyCompararer;
        bool IList.IsFixedSize => false;
        object IList.this[int index]
        {
            get => ElementAt(index);
            set
            {
                if (value is T val)
                {
                    lock (SyncRoot) { BaseList[index] = val; }
                }
                else throw new ArgumentException($"Only values of type {typeof(T)} can be added to this list.");
            }
        }

        //CONSTRUCTORS
        public AutoCollection(int capacity, bool synchronized) : this(capacity, synchronized, EqualityComparer<object>.Default) { }
        public AutoCollection() : this(0, false, EqualityComparer<object>.Default) { }
        public AutoCollection(int capacity) : this(capacity, false, EqualityComparer<T>.Default) { }
        public AutoCollection(IEqualityComparer keyCompararer) : this(0, false, keyCompararer) { }
        public AutoCollection(int capacity, bool synchronized, IEqualityComparer keyCompararer)
        {
            this.keyCompararer = keyCompararer;

            if (IsSynchronized = synchronized) { BaseList = new SyncedList<T>(new List<T>(capacity)); }
            else { BaseList = new List<T>(capacity); }
        }

        //PUBIC FUNCTIONS
        public int IndexOfKey(object key)
        {
            int x = -1;

            foreach (ICollectable val in BaseList)
            {
                x++;
                if (keyCompararer.Equals(key, val.Key)) { return x; }
            }

            return -1;
        }
        public bool ContainsKey(object key)
        {
            foreach (ICollectable val in BaseList)
            {
                if (keyCompararer.Equals(key, val.Key)) { return true; }
            }

            return false;
        }
        public void Add(T value)
        {
            lock (SyncRoot)
            {
                OnInsert(value);

                BaseList.Add(value);

                OnInsertComplete(BaseList.Count - 1, value);
            }            
        }
        public void AddRange(IEnumerable<T> vals)
        {
            if (vals.Count() == 0) return;

            lock (SyncRoot)
            {
                foreach (T val in vals)
                {
                    OnInsert(val);
                }

                foreach (T val in vals)
                {
                    BaseList.Add(val);
                    OnInsertComplete(BaseList.Count - 1, val);
                }
            }
        }
        public void Insert(int index, T value)
        {
            lock (SyncRoot)
            {
                OnInsert(value);

                BaseList.Insert(index, value);

                OnInsertComplete(index, value);
            }            
        }
        public bool Remove(T value)
        {   
            lock (SyncRoot) 
            {
                int index = IndexOf(value);
                if (index == -1) return false;

                OnRemove(index, value);

                BaseList.RemoveAt(index);
            }            

            OnRemoveComplete(value);

            return true;
        }
        public bool Remove(object key)
        {
            foreach (ICollectable val in BaseList)
            {
                if (keyCompararer.Equals(key, val.Key)) { return BaseList.Remove((T)val); }
            }

            return false;
        }
        public void RemoveAt(int index)
        {
            T val;

            lock (SyncRoot) 
            {
                val = BaseList[index];
                OnRemove(index, val);
                BaseList.RemoveAt(index);
            }            

            OnRemoveComplete(val);
        }
        public void Clear()
        {            
            foreach (T val in BaseList)
            {
                val.Handle.OnRemoved(true);
                val.Handle.KeyChanged -= Value_KeyChanged;
            } 

            BaseList.Clear();

            Cleared?.Invoke(this, EventArgs.Empty);
        }
        public int IndexOf(T value) => BaseList.IndexOf(value);
        public bool Contains(T value) => BaseList.Contains(value);
        public T ElementAtKey(object key)
        {
            foreach (ICollectable val in BaseList)
            {
                if (keyCompararer.Equals(key, val.Key)) { return (T)val; }
            }

            throw new KeyNotFoundException($"The key '{key}' is not in this collection.");
        }
        public T ElementAt(int index) => BaseList[index];
        public IReadOnlyAutoCollection<T> AsReadOnly() => new ReadOnlyAutoCollectionWrapper<T>(this);
        public IReadOnlyTypedAutoCollection<keyT, T> AsTypedReadOnly<keyT>() => new ReadOnlyTypedCollectionWrapper<keyT, T>(this);
        public ITypedAutoCollection<keyT, T> AsTypedKeyCollection<keyT>() => new TypedCollectionWrapper<keyT, T>(this);
        public IEnumerable<object> GetKeys()
        {
            lock (SyncRoot)
            {
                if (Count == 0) return new object[0];

                var ret = new List<object>(Count);

                foreach (ICollectable coll in BaseList) { ret.Add(coll.Key); }

                return ret;
            }
        }
        public IEnumerable<T> GetValues() => BaseList;
        public void CopyTo(T[] arr, int arrayIndex) => BaseList.CopyTo(arr, arrayIndex);

        //EVENT HANDLERS
        private void Value_KeyChanged(CollectionHandle handle, EventArgs args)
        {
            bool found = false;
            int x = 0, index = -1;

            foreach (ICollectable val in BaseList)
            {
                if (keyCompararer.Equals(handle.Key, val.Key))
                {
                    if (found) { throw new KeyAlreadyExistsException(handle.Key); }
                    else { found = true; index = x; }
                }

                x++;
            }

            OnKeyChanged(handle.Key, (T)handle.Owner);

            KeyChanged?.Invoke(this, new CollectionEventArgs(handle.Owner, handle.Key, index));            
        }

        //PRIVATE METHODS
        private void OnInsertComplete(int index, T value)
        {
            OnAdded(index, value.Key, value);

            AfterAdd?.Invoke(this, new CollectionEventArgs(value, value.Key, index));
        }
        private void OnInsert(T value)
        {
            if (value == null) throw new ArgumentNullException("value", "Cannot add a null-value to collection.");

            if (value.Handle is null) throw new HandleIsNullException(value);

            if (value.Handle.IsInCollection) throw new AlreadyInCollectionException(value);

            if (value.Key is null) throw new KeyIsNullException(value);

            if (ContainsKey(value.Key)) throw new KeyAlreadyExistsException(value.Key);

            var cea = new CollectionCancelEventArgs(value, value.Key);

            BeforeAdd?.Invoke(this, cea);

            if (cea.Cancel)
            {
                if (IgnoreCancel) { throw new CollectionException("This collection is set to ignore calls to cancel adding or deleting items."); }
                else { return; }        
            }

            value.Handle.OnAdded(this, value);
            value.Handle.KeyChanged += Value_KeyChanged;
        }
        private void OnRemoveComplete(T value)
        {
            OnRemoved(value);

            AfterRemove?.Invoke(this, new CollectionEventArgs(value, value.Handle.Key));
        }
        private void OnRemove(int index, T value)
        {
            var cea = new CollectionCancelEventArgs(value, value.Key, index);

            BeforeRemove?.Invoke(this, cea);

            if (cea.Cancel)
            {
                if (IgnoreCancel) { throw new CollectionException("This collection is set to ignore calls to cancel adding or deleting items."); }
                else { return; }        
            }

            value.Handle.OnRemoved(false);
            value.Handle.KeyChanged -= Value_KeyChanged;
        }

        //PROTECTED FUNCTIONS
        protected virtual void OnAdded(int index, object key, T value) { }
        protected virtual void OnRemoved(T value) { }
        protected virtual void OnKeyChanged(object key, T value) { }

        //INTERFACE IMPLIMENTATIONS
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => BaseList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)BaseList).GetEnumerator();
        int IList<T>.IndexOf(T value) => IndexOf(value);
        int IReadOnlyAutoCollection<T>.IndexOf(T value) => IndexOf(value);
        ICollectable IReadOnlyAutoCollection.GetValue(object key) => this[key];
        object IReadOnlyAutoCollection.GetValue(int index) => this[index];
        object IReadOnlyAutoCollection.GetKey(int index) => this[index].Key;
        void ICollection.CopyTo(Array array, int index)
        {
            lock (SyncRoot)
            {
                ((ICollection)BaseList).CopyTo(array, index);
            }
        }
        int IList.IndexOf(object value)
        {
            if (value is T val) { return IndexOf(val); }
            else { return -1; }
        }
        int IReadOnlyAutoCollection.IndexOf(ICollectable value)
        {
            if (value is T val) { return IndexOf(val); }
            else { return -1; }
        }
        int IList.Add(object value)
        {
            if (value is T val) 
            {
                Add(val);
                return BaseList.Count - 1;
            }
            else { return -1; }
        }
        bool IList.Contains(object value)
        {
            if (value is T val)
            {
                return Contains(val);
            }
            else { return false; }
        }
        void IList.Insert(int index, object value)
        {
            if (value is T val)
            {
                Insert(index, val);
            }
            else throw new ArgumentException($"Only values of type {typeof(T)} can be added to this list.");
        }
        void IList.Remove(object value)
        {
            if (value is T val)
            {
                Remove(val);
            }
            else throw new ArgumentException($"Value not found.");
        }
    }
}
