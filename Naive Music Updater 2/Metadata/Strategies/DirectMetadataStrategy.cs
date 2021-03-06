namespace NaiveMusicUpdater;

public class DirectMetadataStrategy : IMetadataStrategy
{
    public readonly IFieldSpec Applier;

    public DirectMetadataStrategy(IFieldSpec applier)
    {
        Applier = applier;
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        return Applier.Apply(item, desired);
    }
}
