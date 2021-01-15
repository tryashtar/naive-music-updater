using System;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class ItemPredicateFactory
    {
        public static IItemPredicate CreateFrom(string str)
        {
            return new ExactItemPredicate(str);
        }

        public static IItemPredicate CreateFrom(Regex regex)
        {
            return new RegexItemPredicate(regex);
        }

        public static IItemPredicate FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return CreateFrom((string)node);
            if (node is YamlMappingNode map)
                return CreateFrom((string)map["regex"]);
            throw new ArgumentException();
        }
    }
}
