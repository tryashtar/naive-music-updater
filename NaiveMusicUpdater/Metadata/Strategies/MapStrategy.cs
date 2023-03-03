namespace NaiveMusicUpdater;

public class MapStrategy : IMetadataStrategy
{
    private readonly Dictionary<MetadataField, IValueSource> Fields;
    public CombineMode Mode { get; }

    public MapStrategy(Dictionary<MetadataField, IValueSource> fields, CombineMode mode)
    {
        Fields = fields;
        Mode = mode;
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var value = source.Get(item);
                if (value != null)
                    meta.Register(field, value);
            }
        }

        return meta;
    }
}