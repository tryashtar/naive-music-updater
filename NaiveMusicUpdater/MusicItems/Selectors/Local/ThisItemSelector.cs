namespace NaiveMusicUpdater;

public class ThisItemSelector : ILocalItemSelector
{
    public static readonly ThisItemSelector Instance = new();

    public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start)
    {
        yield return start;
    }

    public bool IsSelectedFrom(IMusicItem start, IMusicItem item)
    {
        return start == item;
    }

    public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start)
    {
        yield break;
    }
}