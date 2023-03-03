namespace NaiveMusicUpdater;

public class RemoveStrategy : IMetadataStrategy
{
    public readonly HashSet<MetadataField> Fields;
    public CombineMode Mode => CombineMode.Replace;

    public RemoveStrategy(HashSet<MetadataField> fields)
    {
        Fields = fields;
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        foreach (var field in Fields)
        {
            if (desired(field))
                meta.Register(field, BlankValue.Instance);
        }

        return meta;
    }
}