namespace NaiveMusicUpdater;

public class RemoveFieldSetter : IFieldSetter
{
    public static readonly RemoveFieldSetter Instance = new();

    public MetadataProperty Get(IMusicItem item)
    {
        return MetadataProperty.Delete();
    }

    public MetadataProperty GetWithContext(IMusicItem item, IValue value)
    {
        // discard context
        return Get(item);
    }
}
