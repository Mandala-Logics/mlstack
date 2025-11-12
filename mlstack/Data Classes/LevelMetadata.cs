using DDEncoder;

internal sealed class LevelMetadata : IEncodable
{
    public string OriginalDirPath { get; }

    public LevelMetadata(string origininalPath)
    {
        OriginalDirPath = origininalPath;
    }

    public LevelMetadata(EncodedObject eo)
    {
        OriginalDirPath = eo.Next<string>();
    }

    void IEncodable.Encode(ref EncodedObject encodedObj)
    {
        encodedObj.Append(OriginalDirPath);
    }
}