namespace NaiveMusicUpdater;

public class RemoveStrategy : IMetadataStrategy
{
    public readonly HashSet<MetadataField> Fields;

    public RemoveStrategy(HashSet<MetadataField> fields)
    {
        Fields = fields;
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        foreach (var field in Fields)
        {
            if (desired(field))
                start.Register(field, BlankValue.Instance);
        }
    }
}