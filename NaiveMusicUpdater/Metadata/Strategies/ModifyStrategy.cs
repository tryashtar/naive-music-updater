namespace NaiveMusicUpdater;

public class ModifyStrategy : IMetadataStrategy
{
    private readonly Dictionary<MetadataField, IValueOperator> Fields;

    public ModifyStrategy(Dictionary<MetadataField, IValueOperator> fields)
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