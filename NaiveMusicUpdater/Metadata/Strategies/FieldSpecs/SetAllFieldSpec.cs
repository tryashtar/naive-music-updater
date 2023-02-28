namespace NaiveMusicUpdater;

public class SetAllFieldSpec : IFieldSpec
{
    public readonly HashSet<MetadataField> Fields;
    public readonly IFieldSetter Setter;

    public SetAllFieldSpec(HashSet<MetadataField> fields, IFieldSetter setter)
    {
        Fields = fields;
        Setter = setter;
    }

    private Metadata ApplyLike(Predicate<MetadataField> desired, Func<IFieldSetter, MetadataProperty> get)
    {
        var meta = new Metadata();
        foreach (var field in Fields)
        {
            if (desired(field))
                meta.Register(field, get(Setter));
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
