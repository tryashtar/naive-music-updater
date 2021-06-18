﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IMetadataStrategy
    {
        Metadata Get(IMusicItem item, Predicate<MetadataField> desired);
    }

    public static class MetadataStrategyFactory
    {
        public static IMetadataStrategy Create(YamlNode node)
        {
            if (node is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum(def: MetadataStrategyType.Field);
                if (type == MetadataStrategyType.Multi)
                    return new RedirectingMetadataStrategy(map);
                else if (type == MetadataStrategyType.Field)
                    return new FieldMapMetadataStrategy(map);
            }
            if (node is YamlSequenceNode list)
                return new MultipleMetadataStrategy(list);
            throw new ArgumentException($"Can't make metadata strategy from {node}");
        }
    }

    public enum MetadataStrategyType
    {
        Multi,
        Field
    }
}
