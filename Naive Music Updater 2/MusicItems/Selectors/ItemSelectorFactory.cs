using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
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
            if (node is YamlScalarNode scalar)
                return new PathItemSelector(scalar.Value);
            if (node is YamlSequenceNode sequence)
                return new MultiItemSelector(sequence);
            if (node is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum<SelectorType>();
                if (type == SelectorType.Path)
                {
                    var predicates = node.Go("path").ToList(x => ItemPredicateFactory.FromNode(x)).ToArray();
                    return new PathItemSelector(predicates);
                }
                else if (type == SelectorType.Subpath)
                {
                    var subpath = node.Go("subpath").Parse(x => ItemSelectorFactory.Create(x));
                    var select = node.Go("select").Parse(x => ItemSelectorFactory.Create(x));
                    return new SubPathItemSelector(subpath, select);
                }
            }
            throw new ArgumentException($"Couldn't make an item selector from {node}");
        }
    }

    public enum SelectorType
    {
        Path,
        Subpath
    }
}
