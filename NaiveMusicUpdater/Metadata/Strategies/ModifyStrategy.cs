namespace NaiveMusicUpdater;

public class ModifyStrategy : IMetadataStrategy
{
    private readonly IReadOnlyDictionary<MetadataField, IValueOperator> Fields;

    public ModifyStrategy(IReadOnlyDictionary<MetadataField, IValueOperator> fields)
    {
        Fields = fields;
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var value = start.Get(field);
                if (value.IsBlank)
                    continue;
                var modified = source.Apply(item, value);
                if (modified != null)
                    start.Register(field, modified);
            }
        }
    }
}