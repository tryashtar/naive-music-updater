using System;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IItemPredicate
    {
        bool Matches(IMusicItem item);
    }

    public static class ItemPredicateFactory
    {
        public static IItemPredicate FromNode(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
                return new ExactItemPredicate(scalar.Value);
            if (node is YamlMappingNode map)
            {
                var regex = map.Go("regex").Parse(x => new Regex(x.String(), RegexOptions.IgnoreCase));
                return new RegexItemPredicate(regex);
            }
            throw new ArgumentException($"Can't make item predicate from {node}");
        }
    }
}
