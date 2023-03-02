namespace NaiveMusicUpdater;

public static class MusicItemUtils
{
    public static Metadata GetMetadata(this IMusicItem item, Predicate<MetadataField> desired)
    {
        var metadata = new Metadata();
        foreach (var parent in item.PathFromRoot())
        {
            foreach (var config in parent.Configs)
            {
                metadata.MergeWith(config.GetMetadata(item, desired));
            }
        }
        return metadata;
    }

    public static IEnumerable<IMusicItem> PathFromRoot(this IMusicItem item)
    {
        var list = new List<IMusicItem>();
        while (item != null)
        {
            list.Add(item);
            item = item.Parent;
        }
        list.Reverse();
        return list;
    }
}
