namespace NaiveMusicUpdater;

// a music item: either a file or folder
// folders can have metadata too, but don't do much with it
public interface IMusicItem
{
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    // configs that directly apply to this item
    List<IMusicItemConfig> Configs { get; }
    MusicLibrary RootLibrary { get; }
}

public static class MusicItemExtensions
{
    public static Metadata GetMetadata(this IMusicItem item, Predicate<MetadataField> desired)
    {
        var metadata = new Metadata();
        foreach (var parent in item.PathFromRoot())
        {
            foreach (var config in parent.Configs)
            {
                config.Apply(metadata, item, desired);
            }
        }

        return metadata;
    }

    // all parent items, starting from root, to the given item
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

    // path relative to root
    public static string StringPathAfterRoot(this IMusicItem item)
    {
        return String.Join(Path.DirectorySeparatorChar.ToString(),
            item.PathFromRoot().Skip(1).Select(x => x.SimpleName));
    }
}