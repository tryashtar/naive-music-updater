namespace NaiveMusicUpdater;

public class MapFieldSpec : IFieldSpec
{
    public readonly Dictionary<MetadataField, IFieldSetter> Fields;

    public MapFieldSpec(Dictionary<MetadataField, IFieldSetter> fields)
    {
        Fields = fields;
    }

    private Metadata ApplyLike(Predicate<MetadataField> desired, Func<IFieldSetter, MetadataProperty> get)
    {
        var meta = new Metadata();
        foreach (var pair in Fields)
        {
            if (desired(pair.Key))
                meta.Register(pair.Key, get(pair.Value));
        }
        return meta;
    }

    public Metadata Apply(IMusicItem item, Predicate<MetadataField> desired)
    {
        return ApplyLike(desired, x => x.Get(item));
    }

    public Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired)
    {
        return ApplyLike(desired, x => x.GetWithContext(item, value));
    }
}
