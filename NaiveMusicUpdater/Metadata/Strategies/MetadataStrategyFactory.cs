namespace NaiveMusicUpdater;

public interface IMetadataStrategy
{
    void Apply(Metadata start, IMusicItem item, Predicate<MetadataField> desired);
}

public static class MetadataStrategyFactory
{
    public static IMetadataStrategy Create(YamlNode yaml)
    {
        switch (yaml)
        {
            case YamlMappingNode map:
            {
                var modify = map.Go("modify").ToDictionary(
                    x => MetadataField.FromID(x.String()!),
                    ValueOperatorFactory.Create);
                if (modify != null)
                {
                    var source = map.Go("source").NullableParse(ValueSourceFactory.Create);
                    return source == null ? new ModifyStrategy(modify) : new ContextStrategy(source, modify);
                }

                var remove = map.Go("remove");
                if (remove != null)
                {
                    return remove.String() == "*"
                        ? new RemoveStrategy(MetadataField.Values.ToHashSet())
                        : new RemoveStrategy(remove.ToListFromStrings(x => MetadataField.FromID(x!))!.ToHashSet());
                }

                Dictionary<MetadataField, IValueSource> direct;
                var mode = map.Go("mode").ToEnum<CombineMode>();
                if (mode != null)
                    direct = map.Go("values").ToDictionary(
                        x => MetadataField.FromID(x.String()!),
                        ValueSourceFactory.Create)!;
                else
                    direct = map.ToDictionary(
                        x => MetadataField.FromID(x.String()!),
                        ValueSourceFactory.Create)!;
                return new MapStrategy(direct, mode ?? CombineMode.Replace);
            }
            case YamlSequenceNode list:
            {
                var substrats = list.ToList(MetadataStrategyFactory.Create);
                return new MultipleStrategy(substrats!);
            }
            default:
                throw new ArgumentException($"Can't make metadata strategy from {yaml}");
        }
    }
}