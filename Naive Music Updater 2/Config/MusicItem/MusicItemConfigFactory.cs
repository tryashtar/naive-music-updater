namespace NaiveMusicUpdater;

public interface IMusicItemConfig
{
    string Location { get; }
    Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired);
    CheckSelectorResults CheckSelectors();
}

public static class MusicItemConfigFactory
{
    public static IMusicItemConfig Create(string file, IMusicItem item)
    {
        var yaml = YamlHelper.ParseFile(file);
        var reverse = yaml.Go("reverse");
        if (reverse != null)
        {
            var type = StringUtils.ParseUnderscoredEnum<ReversalType>(reverse.String());
            if (item is MusicFolder folder)
            {
                foreach (var song in folder.GetAllSongs())
                {
                    var current = song.GetEmbeddedMetadata(MetadataField.All);
                    var incoming = song.GetMetadata(MetadataField.All);
                }
            }
        }
        return new MusicItemConfig(file, item, yaml);
    }
}

public enum ReversalType
{
    Minimal,
    Full
}
