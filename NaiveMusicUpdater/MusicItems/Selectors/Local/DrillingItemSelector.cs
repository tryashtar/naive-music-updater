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
        var arr = path.ToArray();
        var range = Range.WithLength(arr.Length);
        return arr[range.Start..range.End].Where(CheckMustBe);
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
        if (MustBe == null)
            return true;
        if (MustBe == MusicItemType.File && item is Song)
            return true;
        if (MustBe == MusicItemType.Folder && item is MusicFolder)
            return true;
        return false;
    }
}

public enum DrillDirection
{
    Down,
    Up
}