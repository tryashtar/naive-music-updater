namespace NaiveMusicUpdater;

public interface IValueSource
{
    IValue Get(IMusicItem item);
}

public static class ValueSourceFactory
{
    public static IValueSource Create(YamlNode yaml)
    {
        if (yaml is YamlScalarNode scalar && scalar.Value != null)
            return new LiteralStringSource(scalar.Value);
        else if (yaml is YamlSequenceNode sequence)
            return new LiteralListSource(sequence.ToStringList());
        else if (yaml is YamlMappingNode map)
        {
            var selector = map.Go("from").Parse(LocalItemSelectorFactory.Create);
            var getter = map.Go("value").NullableParse(MusicItemGetterFactory.Create) ?? CleanNameGetter.Instance;
            var modifier = map.Go("modify").NullableParse(ValueOperatorFactory.Create);
            return new MusicItemSource(selector, getter, modifier);
        }
        throw new ArgumentException($"Can't make value resolver from {yaml}");
    }
}
