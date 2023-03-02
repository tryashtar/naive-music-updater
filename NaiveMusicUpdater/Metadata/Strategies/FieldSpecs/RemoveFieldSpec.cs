namespace NaiveMusicUpdater;

public class RemoveFieldSpec : IFieldSpec
{
    public readonly HashSet<MetadataField> Fields;

    public RemoveFieldSpec(HashSet<MetadataField> fields)
    {
        Fields = fields;
    }

    public Metadata Apply(IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        foreach (var field in Fields)
        {
            if (desired(field))
                meta.Register(field, BlankValue.Instance);
        }
        return meta;
    }

    public Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired)
    {
        return Apply(item, desired);
    }
}
