namespace NaiveMusicUpdater;

public class ParentItemSelector : ISingleItemSelector
{
    public readonly int Up;
    public readonly MusicItemType? MustBe;
    public ParentItemSelector(int up, MusicItemType? must_be = null)
    {
        Up = up;
        MustBe = must_be;
    }

    public IMusicItem? SelectFrom(IMusicItem value)
    {
        for (int i = 0; i < Up; i++)
        {
            if (value.Parent == null)
                return null;
            value = value.Parent;
        }
        return CheckMustBe(value);
    }

    private IMusicItem? CheckMustBe(IMusicItem item)
    {
        if (MustBe == null)
            return item;
        if (MustBe == MusicItemType.File && item is Song)
            return item;
        if (MustBe == MusicItemType.Folder && item is MusicFolder)
            return item;
        return null;
    }
}
