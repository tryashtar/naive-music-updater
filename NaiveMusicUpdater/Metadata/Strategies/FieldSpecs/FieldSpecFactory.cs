namespace NaiveMusicUpdater;

public interface IFieldSpec
{
    Metadata Apply(IMusicItem item, Predicate<MetadataField> desired);
    Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired);
}

public static class FieldSpecFactory
{
    public static IFieldSpec Create(YamlNode yaml, bool has_context)
    {
        if (yaml is YamlMappingNode map)
        {
            var remove = yaml.Go("remove");
            if (remove != null)
            {
                IEnumerable<MetadataField> set;
                if (remove is YamlScalarNode { Value: "*" })
                    set = MetadataField.Values;
                else
                    set = remove.ToList(x => MetadataField.FromID(x.String()));
                return new RemoveFieldSpec(set.ToHashSet());
            }
            else
            {
                var direct = yaml.ToDictionary(
                    x => MetadataField.FromID(x.String()),
                    x => FieldSetterFactory.Create(x, has_context)
                );
                return new MapFieldSpec(direct);
            }
        }
        throw new ArgumentException($"Can't make field spec from {yaml}");
    }
}
