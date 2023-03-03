namespace NaiveMusicUpdater;

public class ContextStrategy : IMetadataStrategy
{
    private readonly IValueSource Context;
    private readonly Dictionary<MetadataField, IValueOperator> Fields;

    public ContextStrategy(IValueSource context, Dictionary<MetadataField, IValueOperator> fields)
    {
        Context = context;
        Fields = fields;
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        var value = Context.Get(item);
        if (value == null)
            return;
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var modified = source.Apply(item, value);
                if (modified != null)
                    meta.Register(field, modified);
            }
        }

        start.MergeWith(meta, CombineMode.Replace);
    }
}