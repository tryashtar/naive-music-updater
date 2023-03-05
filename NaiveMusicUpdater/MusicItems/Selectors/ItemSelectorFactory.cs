namespace NaiveMusicUpdater;

public interface IItemSelector
{
    IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start);
    bool IsSelectedFrom(IMusicItem start, IMusicItem item);
    IEnumerable<IItemSelector> UnusedFrom(IMusicItem start);
}

public static class ItemSelectorFactory
{
    public static IItemSelector Create(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode { Value: { } } scalar:
                return new PathItemSelector(scalar.Value);
            case YamlSequenceNode sequence:
            {
                var subselectors = sequence.ToList(ItemSelectorFactory.Create);
                return new MultiItemSelector(subselectors);
            }
            case YamlMappingNode map:
            {
                var path = node.Go("path");
                if (path != null)
                {
                    var predicates = path.ToList(ItemPredicateFactory.Create).ToArray();
                    return new PathItemSelector(predicates);
                }

                var subpath = node.Go("subpath").NullableParse(ItemSelectorFactory.Create);
                if (subpath != null)
                {
                    var select = node.Go("select").Parse(ItemSelectorFactory.Create);
                    return new SubPathItemSelector(subpath, select);
                }

                break;
            }
        }

        throw new ArgumentException($"Can't make item selector from {node}");
    }
}