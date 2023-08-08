namespace NaiveMusicUpdater;

public class DrillingItemSelector : ILocalItemSelector
{
    public readonly DrillDirection Direction;
    public readonly Range Range;
    public readonly MusicItemType? MustBe;

    public DrillingItemSelector(DrillDirection dir, Range up, MusicItemType? must_be = null)
    {
        Direction = dir;
        Range = up;
        MustBe = must_be;
    }

    public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start)
    {
        var path = start.PathFromRoot();
        if (Direction == DrillDirection.Up)
            path = path.Reverse();
        var arr = RangeFactory.Get(path.ToArray(), Range, OutofBoundsDecision.Clamp);
        if (arr == null)
            return Enumerable.Empty<IMusicItem>();
        return arr.Where(CheckMustBe);
    }

    public bool IsSelectedFrom(IMusicItem start, IMusicItem item)
    {
        return AllMatchesFrom(start).Contains(item);
    }

    public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start)
    {
        yield break;
    }

    private bool CheckMustBe(IMusicItem item)
    {
        switch (MustBe)
        {
            case null:
            case MusicItemType.File when item is Song:
            case MusicItemType.Folder when item is MusicFolder:
                return true;
            default:
                return false;
        }
    }
}

public enum DrillDirection
{
    Down,
    Up
}