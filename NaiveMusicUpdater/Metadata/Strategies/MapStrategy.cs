namespace NaiveMusicUpdater;

public class MapStrategy : IMetadataStrategy
{
    private readonly IReadOnlyDictionary<MetadataField, IValueSource> Fields;
    public CombineMode Mode { get; }

    public MapStrategy(IReadOnlyDictionary<MetadataField, IValueSource> fields, CombineMode mode)
    {
        Fields = fields;
        Mode = mode;
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var value = source.Get(item);
                if (value != null)
                    start.Combine(field, value, Mode);
            }
        }
    }
}