namespace NaiveMusicUpdater;

public interface IItemPredicate
{
    bool Matches(IMusicItem item);
}

public static class ItemPredicateFactory
{
    public static IItemPredicate Create(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode { Value: { } } scalar:
                return new ExactItemPredicate(scalar.Value);
            case YamlMappingNode map:
            {
                var regex = map.Go("regex").Parse(x => new Regex(x.String(), RegexOptions.IgnoreCase));
                return new RegexItemPredicate(regex);
            }
            default:
                throw new ArgumentException($"Can't make item predicate from {node}");
        }
    }
}
