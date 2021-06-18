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
                var type = scalar.ToEnum<SimpleSource>();
                if (type == SimpleSource.This || type == SimpleSource.Self)
                    return ThisItemSelector.Instance;
            }
            else if (node is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum<AdvancedSourceType>();
                if (type == AdvancedSourceType.Parent)
                {
                    int up = map.Go("up").Int().Value;
                    return new ParentItemSelector(up);
                }
                else if (type == AdvancedSourceType.Root)
                {
                    int down = map.Go("down").Int().Value;
                    return new RootItemSelector(down);
                }
                else if (type == AdvancedSourceType.Selector)
                {
                    var selector = map.Go("selector").Parse(x => ItemSelectorFactory.Create(x));
                    return new SingleSelectorWrapper(selector);
                }
            }
            throw new ArgumentException($"Couldn't make a single-item selector from {node}");
        }
    }

    public enum SimpleSource
    {
        This,
        Self
    }

    public enum AdvancedSourceType
    {
        Parent,
        Root,
        Selector
    }
}
