namespace NaiveMusicUpdater;

public class ContextStrategy : IMetadataStrategy
{
    private readonly IValueSource Context;
    private readonly Dictionary<MetadataField,IValueOperator> Fields;
    public CombineMode Mode => CombineMode.Replace;

    public ContextStrategy(IValueSource context, Dictionary<MetadataField,IValueOperator> fields)
    {
        Context = context;
        Fields = fields;
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        var value = Context.Get(item);
        foreach (var (field, source) in Fields)
        {
            if (desired(field))
            {
                var modified = source.Apply(item, value);
                if (value != null)
                    meta.Register(field, value);
            }
        }
        return meta;
    }
}