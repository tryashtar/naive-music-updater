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
                var apply = map.Go("apply").Parse(x => FieldSpecFactory.Create(x, true));
                return new RedirectingMetadataStrategy(source, apply);
            }
            else
            {
                var apply = map.Parse(x => FieldSpecFactory.Create(x, false));
                return new DirectMetadataStrategy(apply);
            }
        }

        if (yaml is YamlSequenceNode list)
        {
            var substrats = list.ToList(MetadataStrategyFactory.Create);
            return new MultipleMetadataStrategy(substrats);
        }

        throw new ArgumentException($"Can't make metadata strategy from {yaml}");
    }
}