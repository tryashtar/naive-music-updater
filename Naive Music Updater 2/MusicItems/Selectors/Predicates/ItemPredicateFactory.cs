using System;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class ItemPredicateFactory
    {
        public static IItemPredicate FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return new ExactItemPredicate((string)node);
            if (node is YamlMappingNode map)
                return new RegexItemPredicate(new Regex((string)map["regex"], RegexOptions.IgnoreCase));
            throw new ArgumentException($"{node} is {node.NodeType}, doesn't work for item predicate");
        }
    }
}
