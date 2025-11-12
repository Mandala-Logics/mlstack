using System;

namespace mlAutoCollection
{
    public delegate void CollectionEventHandler(object sender, CollectionEventArgs args);
    public delegate void CollectionCancelEventHandler(object sender, CollectionCancelEventArgs args);
    public delegate void KeyChangedEventHandler(CollectionHandle sender, EventArgs args);

    public class CollectionEventArgs : EventArgs
    {
        public ICollectable Value { get; }
        public object Key { get; }
        public int Index { get; }

        public CollectionEventArgs(ICollectable value, object key, int index)
        {
            Value = value;
            Key = key;
            Index = index;
        }

        public CollectionEventArgs(ICollectable value, object key)
        {
            Value = value;
            Key = key;
            Index = -1;
        }
    }

    public class CollectionCancelEventArgs : EventArgs
    {
        public ICollectable Value { get; }
        public object Key { get; }
        public int Index { get; }
        public bool Cancel { get; set; } = false;

        public CollectionCancelEventArgs(ICollectable value, object key, int index)
        {
            Value = value;
            Key = key;
            Index = index;
        }

        public CollectionCancelEventArgs(ICollectable value, object key)
        {
            Value = value;
            Key = key;
            Index = -1;
        }
    }
}
