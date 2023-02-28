namespace NaiveMusicUpdater;

public class TargetedStrategy : IItemSelector, IMetadataStrategy
{
    public readonly IItemSelector Selector;
    public readonly IMetadataStrategy Strategy;

    public TargetedStrategy(IItemSelector selector, IMetadataStrategy strategy)
    {
        Selector = selector;
        Strategy = strategy;
    }

    public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start) => Selector.AllMatchesFrom(start);
    public Metadata Get(IMusicItem item, Predicate<MetadataField> desired) => Strategy.Get(item, desired);
    public bool IsSelectedFrom(IMusicItem start, IMusicItem item) => Selector.IsSelectedFrom(start, item);
    public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start) => Selector.UnusedFrom(start);
}
