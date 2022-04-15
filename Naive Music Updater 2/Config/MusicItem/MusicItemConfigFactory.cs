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
                var item_depth = item.PathFromRoot().Count();
                var sets = new List<TargetedStrategy>();
                foreach (var song in folder.GetAllSongs())
                {
                    var path = song.PathFromRoot().Skip(item_depth).Select(x => new ExactItemPredicate(x.SimpleName)).ToArray();
                    var current = song.GetEmbeddedMetadata(MetadataField.All);
                    var incoming = song.GetMetadata(MetadataField.All);
                    var spec = new Dictionary<MetadataField, IFieldSetter>();
                    foreach (var field in MetadataField.Values)
                    {
                        spec[field] = new DirectValueSourceFieldSetter(new LiteralMetadataSource(incoming.Get(field)));
                    }
                    var strategy = new TargetedStrategy(new PathItemSelector(path), new DirectMetadataStrategy(new MapFieldSpec(spec)));
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
