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
        if (node is YamlScalarNode scalar && scalar.Value != null)
            return new PathItemSelector(scalar.Value);
        if (node is YamlSequenceNode sequence)
        {
            var subselectors = sequence.ToList(x => ItemSelectorFactory.Create(x));
            return new MultiItemSelector(subselectors);
        }
        if (node is YamlMappingNode map)
        {
            var path = node.Go("path");
            if (path != null)
            {
                var predicates = path.ToList(x => ItemPredicateFactory.FromNode(x)).ToArray();
                return new PathItemSelector(predicates);
            }
            var subpath = node.Go("subpath").NullableParse(x => ItemSelectorFactory.Create(x));
            if (subpath != null)
            {
                var select = node.Go("select").Parse(x => ItemSelectorFactory.Create(x));
                return new SubPathItemSelector(subpath, select);
            }
        }
        throw new ArgumentException($"Can't make item selector from {node}");
    }
}
