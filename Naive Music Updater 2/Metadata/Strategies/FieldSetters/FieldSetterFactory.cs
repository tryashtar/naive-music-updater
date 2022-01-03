namespace NaiveMusicUpdater;

public interface IFieldSetter
{
    MetadataProperty Get(IMusicItem item);
    MetadataProperty GetWithContext(IMusicItem item, IValue value);
}

public static class FieldSetterFactory
{
    public static IFieldSetter Create(YamlNode yaml, bool has_context)
    {
        if (yaml is YamlMappingNode map)
        {
            var mode = map.Go("mode").ToEnum(def: CombineMode.Replace);
            if (mode == CombineMode.Remove)
                return RemoveFieldSetter.Instance;
            var modify = map.Go("modify").NullableParse(ValueOperatorFactory.Create);
            var source = map.Go("source").NullableParse(ValueSourceFactory.Create);
            if (source != null)
                return new ModeValueSourceFieldSetter(mode, source, modify);
            if (has_context && modify != null)
                return new ModeContextFieldSetter(mode, modify);
        }
        var direct_source = ValueSourceFactory.Create(yaml);
        return new DirectValueSourceFieldSetter(direct_source);
        throw new ArgumentException($"Can't make field setter from {yaml}");
    }
}
