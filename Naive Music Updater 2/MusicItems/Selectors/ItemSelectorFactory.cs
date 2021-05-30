using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class ItemSelectorFactory
    {
        public static IItemSelector FromNode(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return new PathItemSelector((string)node);
            if (node.NodeType == YamlNodeType.Sequence)
                return new MultiItemSelector((YamlSequenceNode)node);
            if (node.NodeType == YamlNodeType.Mapping)
            {
                string type = (string)node["type"];
                if (type == "path")
                {
                    var path = (YamlSequenceNode)node["path"];
                    var predicates = path.Children.Select(x => ItemPredicateFactory.FromNode(x)).ToArray();
                    return new PathItemSelector(predicates);
                }
                else if (type == "subpath")
                {
                    var subpath = ItemSelectorFactory.FromNode(node["subpath"]);
                    var select = ItemSelectorFactory.FromNode(node["select"]);
                    return new SubPathItemSelector(subpath, select);
                }
            }
            throw new ArgumentException($"Couldn't make an item selector from {node}");
        }
    }
}
