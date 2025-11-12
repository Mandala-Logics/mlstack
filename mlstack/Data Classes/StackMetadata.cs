using DDEncoder;

internal sealed class StackMetadata : IEncodable

{
    public string LastStackedDir { get; }

    public StackMetadata(string lastDir)
    {
        LastStackedDir = lastDir;
    }

    public StackMetadata(EncodedObject eo)
    {
        LastStackedDir = eo.Next<string>();
    }

    void IEncodable.Encode(ref EncodedObject encodedObj)
    {
        encodedObj.Append(LastStackedDir);
    }
}