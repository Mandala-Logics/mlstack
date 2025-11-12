namespace mlAutoCollection
{
    public interface ICollectable
    {
        object Key { get; }
        CollectionHandle Handle { get; }
    }
}
