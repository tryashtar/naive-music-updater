namespace NaiveMusicUpdater;

public class ContextStrategy : IMetadataStrategy
{
    private readonly IValueSource Context;
    private readonly IReadOnlyDictionary<MetadataField, IValueOperator> Fields;

    public ContextStrategy(IValueSource context, IReadOnlyDictionary<MetadataField, IValueOperator> fields)
    {
        Context = context;
        Fields = fields;
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        var value = Context.Get(item);
        if (value == null)
            return;
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var modified = source.Apply(item, value);
                if (modified != null)
                    start.Register(field, modified);
            }
        }
    }
}