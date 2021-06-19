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
                var must = map.Go("must_be").ToEnum<MusicItemType>();
                var up = map.Go("up").Int();
                if (up != null)
                    return new ParentItemSelector(up.Value, must);
                var down = map.Go("from_root").Int();
                if (down != null)
                    return new RootItemSelector(down.Value, must);
                var selector = map.Go("selector").NullableParse(x => ItemSelectorFactory.Create(x));
                if (selector != null)
                    return new SingleSelectorWrapper(selector);
            }
            throw new ArgumentException($"Can't make single-item selector from {node}");
        }
    }

    public enum SimpleSource
    {
        This,
        Self
    }

    public enum MusicItemType
    {
        File,
        Folder
    }
}
