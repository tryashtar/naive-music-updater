namespace NaiveMusicUpdater;

public class MultipleStrategy : IMetadataStrategy
{
    private readonly List<IMetadataStrategy> Substrategies;
    public CombineMode Mode => CombineMode.Replace;

    public MultipleStrategy(IEnumerable<IMetadataStrategy> strategies)
    {
        Substrategies = strategies.ToList();
    }

    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        foreach (var strat in Substrategies)
        {
            meta.MergeWith(strat.Get(item, desired), strat.Mode);
        }

        return meta;
    }
}
