using System;

namespace mlAutoCollection
{
    public sealed class CollectionHandle
    {
        public static Random Random = new Random();

        //EVENTS
        internal event KeyChangedEventHandler KeyChanged;
        public event EventHandler Removed;
        public event EventHandler Added;

        //PUBLIC PROPERTIES
        public IReadOnlyAutoCollection Collection { get; private set; }
        public bool IsInCollection => Collection != default;
        public int Hash { get; } = Random.Next();

        //INTERNAL PROPERTIES
        internal ICollectable Owner { get; private set; }
        internal object Key => Owner.Key;

        //PRIVATE PROPERTIES
        private object lastKey;

        //PUBLIC METHODS
        public bool IsKeyAllowed(object newKey)
        {
            lastKey = Key;

            if (!IsInCollection) { return true; }
            else
            {
                if (Collection.KeyComparaer.Equals(newKey, Key)) { return true; }
                else if (Collection.ContainsKey(newKey)) { return false; }
                else { return true; }
            }            
        }
        public void OnKeyChanged()
        {
            if (!IsInCollection) { return; }

            if (Collection.KeyComparaer.Equals(lastKey, Key)) { return; }

            KeyChanged?.Invoke(this, EventArgs.Empty);

            lastKey = Key;
        }

        //INTERNAL METHODS
        internal void OnRemoved(bool clearing)
        {
            lastKey = Key;
            Collection = default;
            if (!clearing) { Removed?.Invoke(this, EventArgs.Empty); }      
        }
        internal void OnAdded(IReadOnlyAutoCollection collection, ICollectable owner)
        {
            Collection = collection;
            Owner = owner;
            lastKey = Key;
            Added?.Invoke(this, EventArgs.Empty);
        }

        //OBJECT OVERRIDES
        public override int GetHashCode() => Hash;
        public override bool Equals(object obj) => obj is CollectionHandle ch && ch.Hash == Hash;
    }
}
