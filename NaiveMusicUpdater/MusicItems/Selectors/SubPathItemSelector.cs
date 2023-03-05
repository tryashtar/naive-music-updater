namespace NaiveMusicUpdater;

public class SubPathItemSelector : IItemSelector
{
    private readonly IItemSelector SubPath;
    private readonly IItemSelector Selector;

    public SubPathItemSelector(IItemSelector subpath, IItemSelector selector)
    {
        SubPath = subpath;
        Selector = selector;
    }

    public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start)
    {
        var subbed = SubPath.AllMatchesFrom(start);
        foreach (var sub in subbed)
        {
            var match = Selector.AllMatchesFrom(sub);
            foreach (var result in match)
            {
                yield return result;
            }
        }
    }

    public bool IsSelectedFrom(IMusicItem start, IMusicItem item)
    {
        var subbed = SubPath.AllMatchesFrom(start);
        return subbed.Any(x => Selector.IsSelectedFrom(x, item));
    }

    public IEnumerable<IItemSelector> UnusedFrom(IMusicItem item)
    {
        foreach (var unused in SubPath.UnusedFrom(item))
        {
            yield return unused;
        }

        var subbed = SubPath.AllMatchesFrom(item);
        foreach (var sub in subbed)
        {
            foreach (var unused in Selector.UnusedFrom(sub))
            {
                yield return unused;
            }
        }
    }
}