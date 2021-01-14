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
    public class MusicItemConfig
    {
        // have to defer these because some of the data needed isn't ready until after constructor is done
        public readonly Func<SongOrder> TrackOrder;
        public readonly Func<IMetadataStrategy> MainStrategy;
        public readonly List<Func<(ItemSelector selector, IMetadataStrategy strategy)>> MetadataStrategies;
        private readonly IMusicItem ConfiguredItem;
        public MusicItemConfig(string file, IMusicItem configured_item)
        {
            ConfiguredItem = configured_item;
            var root = YamlHelper.ParseFile(file);
            if (root != null)
            {
                var order = root.TryGet("order");
                if (order != null && configured_item is MusicFolder folder)
                    TrackOrder = () => SongOrderFactory.FromNode(order, folder);
                var local = root.TryGet("this");
                if (local != null)
                    MainStrategy = () => LiteralOrReference(local);
                var set = root.TryGet("set") as YamlMappingNode;
                if (set != null)
                    MetadataStrategies = set.Children.Select(x => (Func<(ItemSelector, IMetadataStrategy)>)(() => ParseStrategy(x.Key, x.Value))).ToList();
            }
            if (MetadataStrategies == null)
                MetadataStrategies = new List<Func<(ItemSelector, IMetadataStrategy)>>();
        }

        public Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired)
        {
            var metadata = new Metadata();
            if (MainStrategy != null)
                metadata.Merge(MainStrategy().Get(item, desired));
            if (TrackOrder != null)
                metadata.Merge(TrackOrder().Get(item));
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

    public static class SongOrderFactory
    {
        public static SongOrder FromNode(YamlNode yaml, MusicFolder folder)
        {
            if (yaml is YamlSequenceNode sequence)
                return new DefinedSongOrder(sequence, folder);
            if (yaml is YamlMappingNode map)
                return new DirectorySongOrder(map);
            throw new ArgumentException();
        }
    }

    public abstract class SongOrder
    {
        public abstract Metadata Get(IMusicItem item);
    }

    public class DefinedSongOrder : SongOrder
    {
        private readonly MusicFolder Folder;
        private readonly List<ItemSelector> DefinedOrder;
        public DefinedSongOrder(YamlSequenceNode yaml, MusicFolder folder)
        {
            Folder = folder;
            DefinedOrder = yaml.Children.Select(x => ItemSelector.FromNode(x)).ToList();
        }

        public override Metadata Get(IMusicItem item)
        {
            var metadata = new Metadata();
            for (int i = 0; i < DefinedOrder.Count; i++)
            {
                if (DefinedOrder[i].IsSelectedFrom(Folder, item))
                {
                    metadata.Register(MetadataField.Track, MetadataProperty.Single((i + 1).ToString(), CombineMode.Replace));
                    metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(DefinedOrder.Count.ToString(), CombineMode.Replace));
                    break;
                }
            }
            return metadata;
        }
    }

    public class DirectorySongOrder : SongOrder
    {
        private readonly SortType Sort;
        public DirectorySongOrder(YamlMappingNode yaml)
        {
            var sort = yaml.TryGet("sort");
            if ((string)sort == "alphabetical")
                Sort = SortType.Alphabetical;
        }

        public override Metadata Get(IMusicItem item)
        {
            List<Song> Sorted = item.Parent.Songs.OrderBy(GetSort()).ToList();
            var metadata = new Metadata();
            for (int i = 0; i < Sorted.Count; i++)
            {
                if (Sorted[i] == item)
                {
                    metadata.Register(MetadataField.Track, MetadataProperty.Single((i + 1).ToString(), CombineMode.Replace));
                    metadata.Register(MetadataField.TrackTotal, MetadataProperty.Single(Sorted.Count.ToString(), CombineMode.Replace));
                    break;
                }
            }
            return metadata;
        }

        private Func<Song, string> GetSort()
        {
            if (Sort == SortType.Alphabetical)
                return x => x.SimpleName;
            throw new ArgumentException();
        }

        private enum SortType
        {
            Alphabetical
        }
    }
}
