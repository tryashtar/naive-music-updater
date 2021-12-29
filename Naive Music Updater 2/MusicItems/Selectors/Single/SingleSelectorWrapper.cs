namespace NaiveMusicUpdater;

public class SingleSelectorWrapper : ISingleItemSelector
{
    public readonly IItemSelector Wrapped;
    public SingleSelectorWrapper(IItemSelector wrapped)
    {
        Wrapped = wrapped;
    }

    public IMusicItem? SelectFrom(IMusicItem value)
    {
        return Wrapped.AllMatchesFrom(value).SingleOrDefault();
    }
}
