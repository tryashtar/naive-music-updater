namespace NaiveMusicUpdater;

public class MultipleStrategy : IMetadataStrategy
{
    private readonly List<IMetadataStrategy> Substrategies;

    public MultipleStrategy(IEnumerable<IMetadataStrategy> strategies)
    {
        Substrategies = strategies.ToList();
    }

    public void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired)
    {
        foreach (var strat in Substrategies)
        {
            strat.Apply(start, item, desired);
        }
    }
}
