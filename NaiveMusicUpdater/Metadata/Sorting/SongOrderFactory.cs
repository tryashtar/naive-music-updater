namespace NaiveMusicUpdater;

public interface ISongOrder
{
    void Apply(Metadata start, IMusicItem item);
}

public static class SongOrderFactory
{
    public static ISongOrder Create(YamlNode yaml, MusicFolder folder)
    {
        var selector = ItemSelectorFactory.Create(yaml);
        return new DefinedSongOrder(selector, folder);
    }
}

public static class DiscOrderFactory
{
    public static ISongOrder Create(YamlNode yaml, MusicFolder folder)
    {
        if (yaml is YamlMappingNode map)
        {
            var dict = new Dictionary<uint, IItemSelector>();
            foreach (var item in map)
            {
                if (uint.TryParse((string)item.Key, out uint n))
                    dict[n] = ItemSelectorFactory.Create(item.Value);
            }
            return new DefinedDiscOrder(dict, folder);
        }
        throw new ArgumentException($"Can't make disc order from {yaml}");
    }
}
