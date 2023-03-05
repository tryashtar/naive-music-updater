namespace NaiveMusicUpdater;

public interface IValueSource
{
    IValue? Get(IMusicItem item);
}

public static class ValueSourceFactory
{
    public static IValueSource Create(YamlNode yaml)
    {
        switch (yaml)
        {
            case YamlScalarNode { Value: { } } scalar:
                return new LiteralValueSource(new StringValue(scalar.Value));
            case YamlSequenceNode sequence:
                return new LiteralValueSource(new ListValue(sequence.ToStringList()));
            case YamlMappingNode map:
            {
                var selector = map.Go("from").Parse(LocalItemSelectorFactory.Create);
                var getter = map.Go("value").NullableParse(MusicItemGetterFactory.Create) ??
                             MusicItemGetterFactory.NameGetters[NameType.CleanName];
                var modifier = map.Go("modify").NullableParse(ValueOperatorFactory.Create);
                return new MusicItemSource(selector, getter, modifier);
            }
            default:
                throw new ArgumentException($"Can't make value resolver from {yaml}");
        }
    }
}