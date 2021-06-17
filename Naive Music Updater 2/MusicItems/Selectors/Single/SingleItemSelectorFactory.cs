using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface ISingleItemSelector
    {
        IMusicItem SelectFrom(IMusicItem start);
    }

    public static class SingleItemSelectorFactory
    {
        public static ISingleItemSelector Create(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                string str = (string)scalar;
                if (str == "this" || str == "self")
                    return ThisItemSelector.Instance;
            }
            else if (node is YamlMappingNode map)
            {
                var type = (string)map["type"];
                if (type == "parent")
                {
                    int up = int.Parse((string)map["up"]);
                    return new ParentItemSelector(up);
                }
                else if (type == "root")
                {
                    int down = int.Parse((string)map["down"]);
                    return new RootItemSelector(down);
                }
                else if (type == "selector")
                    return new SingleSelectorWrapper(ItemSelectorFactory.Create(map["selector"]));
            }
            throw new ArgumentException($"Couldn't make a single-item selector from {node}");
        }
    }
}
