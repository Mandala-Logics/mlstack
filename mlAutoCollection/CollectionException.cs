using System;

namespace mlAutoCollection
{
    public class CollectionException : Exception
    {
        public CollectionException(string message) : base(message) { }
    }

    public class KeyAlreadyExistsException : CollectionException
    {
        public KeyAlreadyExistsException(object key) : base($"This key ({key}) already exists in this collection.") { }
    }

    public class AlreadyInCollectionException : CollectionException
    {
        public AlreadyInCollectionException(object val) : base("This object is already in a collection: " + val.ToString()) { }
    }

    public class KeyIsNullException : CollectionException
    {
        public KeyIsNullException(object val) : base("This value has a null key: " + val.ToString()) { }
    }

    public class HandleIsNullException : CollectionException
    {
        public HandleIsNullException(object val) : base("This value has a null handle: " + val.ToString()) { }
    }

    public class ValueNotInCollectionException : CollectionException
    {
        public ValueNotInCollectionException(object val) : base("This value is not in the collection: " + val.ToString()) { }
    }
}
