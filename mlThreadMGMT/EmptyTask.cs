namespace mlThreadMGMT
{
    public sealed class EmptyTask : ThreadTask
    {
        internal EmptyTask()
        {
            Complete();
        }
    }
}