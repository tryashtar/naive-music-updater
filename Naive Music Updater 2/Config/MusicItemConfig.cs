﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NaiveMusicUpdater
{
    public class MusicItemConfig
    {
        public readonly string Location;
        // have to defer these because some of the data needed isn't ready until after constructor is done
        public readonly Func<SongOrder> TrackOrder;
        public readonly Func<IMetadataStrategy> SongsStrategy;
        public readonly Func<IMetadataStrategy> FoldersStrategy;
        public readonly List<Func<(ItemSelector selector, IMetadataStrategy strategy)>> MetadataStrategies;
        public readonly List<Func<(List<ItemSelector> selectors, IMetadataStrategy strategy)>> SharedStrategies;
        private readonly IMusicItem ConfiguredItem;
        public MusicItemConfig(string file, IMusicItem configured_item)
        {
            Location = file;
            ConfiguredItem = configured_item;
            var root = YamlHelper.ParseFile(file);
            if (root != null)
            {
                var order = root.TryGet("order");
                if (order != null && configured_item is MusicFolder folder)
                    TrackOrder = () => SongOrderFactory.FromNode(order, folder);
                var songs = root.TryGet("songs");
                if (songs != null)
                    SongsStrategy = () => LiteralOrReference(songs);
                var folders = root.TryGet("folders");
                if (folders != null)
                    FoldersStrategy = () => LiteralOrReference(folders);
                var set = root.TryGet("set") as YamlMappingNode;
                if (set != null)
                    MetadataStrategies = set.Children.Select(x => (Func<(ItemSelector, IMetadataStrategy)>)(() => ParseStrategy(x.Key, x.Value))).ToList();
                var shared = root.TryGet("set all") as YamlSequenceNode;
                if (shared != null)
                {
                    SharedStrategies = new List<Func<(List<ItemSelector> selectors, IMetadataStrategy strategy)>>();
                    foreach (var item in shared)
                    {
                        var names = item.TryGet("names") as YamlSequenceNode;
                        var setting = item.TryGet("set");
                        SharedStrategies.Add(() => ParseMultiple(names, setting));
                    }
                }
            }
            if (MetadataStrategies == null)
                MetadataStrategies = new List<Func<(ItemSelector, IMetadataStrategy)>>();
            if (SharedStrategies == null)
                SharedStrategies = new List<Func<(List<ItemSelector> selectors, IMetadataStrategy strategy)>>();
        }

        public Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired)
        {
            var metadata = new Metadata();
            if (SongsStrategy != null && item is Song)
                metadata.Merge(SongsStrategy().Get(item, desired));
            if (FoldersStrategy != null && item is MusicFolder)
                metadata.Merge(FoldersStrategy().Get(item, desired));
            if (TrackOrder != null && item is Song)
                metadata.Merge(TrackOrder().Get(item));
            foreach (var strat in SharedStrategies)
            {
                var (selectors, strategy) = strat();
                if (selectors.Any(x => x.IsSelectedFrom(ConfiguredItem, item)))
                    metadata.Merge(strategy.Get(item, desired));
            }
            foreach (var strat in MetadataStrategies)
            {
                var (selector, strategy) = strat();
                if (selector.IsSelectedFrom(ConfiguredItem, item))
                    metadata.Merge(strategy.Get(item, desired));
            }
            return metadata;
        }

        private (ItemSelector selector, IMetadataStrategy strategy) ParseStrategy(YamlNode key, YamlNode value)
        {
            var selector = ItemSelector.FromNode(key);
            var strategy = LiteralOrReference(value);
            return (selector, strategy);
        }

        private (List<ItemSelector> selectors, IMetadataStrategy strategy) ParseMultiple(YamlSequenceNode names, YamlNode value)
        {
            var selectors = names.Select(x => ItemSelector.FromNode(x)).ToList();
            var strategy = LiteralOrReference(value);
            return (selectors, strategy);
        }

        private IMetadataStrategy LiteralOrReference(YamlNode node)
        {
            if (node.NodeType == YamlNodeType.Scalar)
                return ConfiguredItem.GlobalCache.Config.GetNamedStrategy((string)node);
            else
            {
                if (node.NodeType == YamlNodeType.Sequence)
                    return new MultipleMetadataStrategy(((YamlSequenceNode)node).Select(LiteralOrReference));
                else
                    return MetadataStrategyFactory.Create(node);
            }
        }
    }
}
