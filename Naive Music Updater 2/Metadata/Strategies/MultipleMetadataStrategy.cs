namespace NaiveMusicUpdater;

public class MultipleMetadataStrategy : IMetadataStrategy
{
    private readonly List<IMetadataStrategy> Substrategies;

    public MultipleMetadataStrategy(IEnumerable<IMetadataStrategy> strategies)
    {
        Substrategies = strategies.ToList();
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var datas = Substrategies.Select(x => x.Get(item, desired));
        return Metadata.FromMany(datas);
    }
}
