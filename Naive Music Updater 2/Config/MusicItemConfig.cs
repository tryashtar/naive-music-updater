using System;
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
    public delegate (ItemSelector selector, IMetadataStrategy strategy) TargetedStrategyProducer();
    public delegate (List<ItemSelector> selectors, IMetadataStrategy strategy) SharedStrategyProducer();
    public delegate SongOrder OrderProducer();
    public delegate IMetadataStrategy StrategyProducer();
    public class MusicItemConfig
    {
        public readonly string Location;
        // have to defer these because some of the data needed isn't ready until after constructor is done
        public readonly OrderProducer TrackOrder;
        public readonly StrategyProducer SongsStrategy;
        public readonly StrategyProducer FoldersStrategy;
        public readonly List<TargetedStrategyProducer> MetadataStrategies;
        public readonly List<SharedStrategyProducer> SharedStrategies;
        private readonly IMusicItem ConfiguredItem;
        public MusicItemConfig(string file, IMusicItem configured_item)
        {
            Location = file;
            ConfiguredItem = configured_item;
            var root = YamlHelper.ParseFile(file);
            if (root != null)
            {
                var order = root.Go("order");
                if (order != null && configured_item is MusicFolder folder)
                    TrackOrder = () => SongOrderFactory.FromNode(order, folder);
                var songs = root.Go("songs");
                if (songs != null)
                    SongsStrategy = () => LiteralOrReference(songs);
                var folders = root.Go("folders");
                if (folders != null)
                    FoldersStrategy = () => LiteralOrReference(folders);
                var set = root.Go("set") as YamlMappingNode;
                if (set != null)
                    MetadataStrategies = set.Children.Select(x => (TargetedStrategyProducer)(() => ParseStrategy(x.Key, x.Value))).ToList();
                var shared = root.Go("set all") as YamlSequenceNode;
                if (shared != null)
                {
                    SharedStrategies = new List<SharedStrategyProducer>();
                    foreach (var item in shared)
                    {
                        var names = item.Go("names") as YamlSequenceNode;
                        var setting = item.Go("set");
                        SharedStrategies.Add(() => ParseMultiple(names, setting));
                    }
                }
            }
            if (MetadataStrategies == null)
                MetadataStrategies = new List<TargetedStrategyProducer>();
            if (SharedStrategies == null)
                SharedStrategies = new List<SharedStrategyProducer>();
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

        public CheckSelectorResults CheckSelectors()
        {
            var results = new CheckSelectorResults();
            var all_selectors = SharedStrategies.SelectMany(x => x().selectors)
                .Concat(MetadataStrategies.Select(x => x().selector));
            if (TrackOrder != null && TrackOrder() is DefinedSongOrder defined)
            {
                all_selectors = all_selectors.Concat(defined.Order);
                results.UnselectedItems.AddRange(defined.UnselectedItems);
            }
            foreach (var selector in all_selectors)
            {
                var find = selector.AllMatchesFrom(ConfiguredItem);
                if (!find.Any())
                    results.UnusedSelectors.Add(selector);
            }
            return results;
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
