namespace NaiveMusicUpdater;

public interface IMetadataStrategy
{
    CombineMode Mode { get; }
    Metadata Get(IMusicItem item, Predicate<MetadataField> desired);
}

public static class MetadataStrategyFactory
{
    public static IMetadataStrategy Create(YamlNode yaml)
    {
        if (yaml is YamlMappingNode map)
        {
            var source = map.Go("source").NullableParse(ValueSourceFactory.Create);
            if (source != null)
            {
                var apply = map.Go("apply").ToDictionary(
                    x => MetadataField.FromID(x.String()),
                    ValueOperatorFactory.Create);
                return new ContextStrategy(source, apply);
            }

            var remove = map.Go("remove").ToListFromStrings(MetadataField.FromID);
            if (remove != null)
                return new RemoveStrategy(remove.ToHashSet());
            Dictionary<MetadataField, IValueSource> direct;
            var mode = map.Go("mode").ToEnum<CombineMode>();
            if (mode != null)
                direct = map.Go("apply").ToDictionary(
                    x => MetadataField.FromID(x.String()),
                    ValueSourceFactory.Create);
            else
                direct = map.ToDictionary(
                    x => MetadataField.FromID(x.String()),
                    ValueSourceFactory.Create);
            return new MapStrategy(direct, mode ?? CombineMode.Replace);
        }

        if (yaml is YamlSequenceNode list)
        {
            var substrats = list.ToList(MetadataStrategyFactory.Create);
            return new MultipleStrategy(substrats);
        }

        throw new ArgumentException($"Can't make metadata strategy from {yaml}");
    }
}