namespace NaiveMusicUpdater;

public class MapFieldSpec : IFieldSpec
{
    public readonly Dictionary<MetadataField, IValueSource> Fields;

    public MapFieldSpec(Dictionary<MetadataField, IValueSource> fields)
    {
        Fields = fields;
    }

    public Metadata Apply(IMusicItem item, Predicate<MetadataField> desired)
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

    public Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired)
    {
        return ApplyLike(desired, x => x.GetWithContext(item, value));
    }
}
