namespace NaiveMusicUpdater;

public class LocalSelectorWrapper : ILocalItemSelector
{
    public readonly IItemSelector Wrapped;
    public LocalSelectorWrapper(IItemSelector wrapped)
    {
        Wrapped = wrapped;
    }

    public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start) => Wrapped.AllMatchesFrom(start);
    public bool IsSelectedFrom(IMusicItem start, IMusicItem item) => Wrapped.IsSelectedFrom(start, item);
    public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start) => Wrapped.UnusedFrom(start);
}
