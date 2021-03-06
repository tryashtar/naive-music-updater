namespace NaiveMusicUpdater;

public class RedirectingMetadataStrategy : IMetadataStrategy
{
    public readonly IValueSource Source;
    public readonly IFieldSpec Applier;

    public RedirectingMetadataStrategy(IValueSource source, IFieldSpec applier)
    {
        Source = source;
        Applier = applier;
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var value = Source.Get(item);
        return Applier.ApplyWithContext(item, value, desired);
    }
}
