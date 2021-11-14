using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class MusicItemConfig
    {
        public readonly string Location;
        public readonly ISongOrder TrackOrder;
        public readonly ISongOrder DiscOrder;
        public readonly IMetadataStrategy SongsStrategy;
        public readonly IMetadataStrategy FoldersStrategy;
        public readonly List<TargetedStrategy> MetadataStrategies;
        public readonly List<TargetedStrategy> SharedStrategies;
        private readonly IMusicItem ConfiguredItem;
        public MusicItemConfig(string file, IMusicItem configured_item)
        {
            Location = file;
            ConfiguredItem = configured_item;
            var yaml = YamlHelper.ParseFile(file);
            if (configured_item is MusicFolder folder)
            {
                DiscOrder = yaml.Go("discs").NullableParse(x => DiscOrderFactory.Create(x, folder));
                if (DiscOrder == null)
                    TrackOrder = yaml.Go("order").NullableParse(x => SongOrderFactory.Create(x, folder));
            }
            SongsStrategy = yaml.Go("songs").NullableParse(x => LiteralOrReference(x));
            FoldersStrategy = yaml.Go("folders").NullableParse(x => LiteralOrReference(x));
            MetadataStrategies = yaml.Go("set").ToList((k, v) => ParseStrategy(k, v)) ?? new();
            SharedStrategies = yaml.Go("set all").ToList(x => ParseMultiple(x.Go("names"), x.Go("set"))) ?? new();
        }

        private TargetedStrategy ParseStrategy(YamlNode key, YamlNode value)
        {
            var selector = ItemSelectorFactory.Create(key);
            var strategy = LiteralOrReference(value);
            return new TargetedStrategy(selector, strategy);
        }

        private TargetedStrategy ParseMultiple(YamlNode names, YamlNode value)
        {
            var selectors = names.ToList(x => ItemSelectorFactory.Create(x));
            var strategy = LiteralOrReference(value);
            return new TargetedStrategy(new MultiItemSelector(selectors), strategy);
        }

        private IMetadataStrategy LiteralOrReference(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
                return ConfiguredItem.GlobalCache.Config.GetNamedStrategy(scalar.Value);
            else
            {
                if (node is YamlSequenceNode sequence)
                    return new MultipleMetadataStrategy(sequence.Select(LiteralOrReference));
                else
                    return MetadataStrategyFactory.Create(node);
            }
        }

        public Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired)
        {
            var metadata = new Metadata();
            if (SongsStrategy != null && item is Song)
                metadata.Merge(SongsStrategy.Get(item, desired));
            if (FoldersStrategy != null && item is MusicFolder)
                metadata.Merge(FoldersStrategy.Get(item, desired));
            if (DiscOrder != null && item is Song)
                metadata.Merge(DiscOrder.Get(item));
            if (TrackOrder != null && item is Song)
                metadata.Merge(TrackOrder.Get(item));
            foreach (var strat in SharedStrategies.Concat(MetadataStrategies))
            {
                if (strat.IsSelectedFrom(ConfiguredItem, item))
                    metadata.Merge(strat.Get(item, desired));
            }
            return metadata;
        }

        public CheckSelectorResults CheckSelectors()
        {
            var results = new CheckSelectorResults();
            IEnumerable<IItemSelector> all_selectors = SharedStrategies.Concat(MetadataStrategies);
            if (TrackOrder is DefinedSongOrder tracks)
            {
                all_selectors = all_selectors.Append(tracks.Order);
                results.UnselectedItems.AddRange(tracks.UnselectedItems);
            }
            if (DiscOrder is DefinedDiscOrder discs)
            {
                all_selectors = all_selectors.Concat(discs.GetSelectors());
                results.UnselectedItems.AddRange(discs.GetUnselectedItems());
            }
            foreach (var selector in all_selectors)
            {
                results.UnusedSelectors.AddRange(selector.UnusedFrom(ConfiguredItem));
            }
            return results;
        }
    }

    public class TargetedStrategy : IItemSelector, IMetadataStrategy
    {
        public readonly IItemSelector Selector;
        public readonly IMetadataStrategy Strategy;

        public TargetedStrategy(IItemSelector selector, IMetadataStrategy strategy)
        {
            Selector = selector;
            Strategy = strategy;
        }

        public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start) => Selector.AllMatchesFrom(start);
        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired) => Strategy.Get(item, desired);
        public bool IsSelectedFrom(IMusicItem start, IMusicItem item) => Selector.IsSelectedFrom(start, item);
        public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start) => Selector.UnusedFrom(start);
    }
}
