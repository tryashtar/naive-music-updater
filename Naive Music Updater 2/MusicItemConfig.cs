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
        public readonly IMusicItem Item;
        public readonly SongOrder TrackOrder;
        // have to defer these because some of the data needed isn't ready until after constructor is done
        public readonly List<Func<(SongPredicate predicate, IMetadataStrategy strategy)>> MetadataStrategies;
        public MusicItemConfig(IMusicItem item, string file)
        {
            Item = item;
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                var stream = new YamlStream();
                stream.Load(reader);
                var root = stream.Documents.SingleOrDefault()?.RootNode;
                var order = root?.TryGet("order");
                if (order != null)
                    TrackOrder = SongOrderFactory.FromNode(Item, order);
                MetadataStrategies = (root?.TryGet("set") as YamlMappingNode)?.Children.Select(x => (Func<(SongPredicate, IMetadataStrategy)>)(() => ParseStrategy(x.Key, x.Value))).ToList() ?? new List<Func<(SongPredicate, IMetadataStrategy)>>();
            }
        }

        private (SongPredicate predicate, IMetadataStrategy strategy) ParseStrategy(YamlNode key, YamlNode value)
        {
            var predicate = SongPredicate.FromNode(key);
            var reference = value.TryGet("reference");
            IMetadataStrategy strategy;
            if (reference != null)
                strategy = Item.GlobalCache.Config.GetNamedStrategy((string)reference);
            else
                strategy = MetadataStrategyFactory.Create(value);
            return (predicate, strategy);
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            var metadata = new List<SongMetadata>();
            if (TrackOrder != null)
                metadata.Add(TrackOrder.GetOrderMetadata(item));
            foreach (var func in MetadataStrategies)
            {
                var (predicate, strategy) = func();
                if (predicate.Matches(Item, item))
                    metadata.Add(strategy.Perform(item));
            }
            return SongMetadata.Merge(metadata);
        }
    }

    public static class SongOrderFactory
    {
        public static SongOrder FromNode(IMusicItem source, YamlNode yaml)
        {
            if (yaml is YamlSequenceNode sequence)
                return new DefinedSongOrder(source, sequence);
            if (yaml is YamlMappingNode map)
            {
                var mode = yaml.TryGet("mode");
                if ((string)mode == "alphabetical")
                    return new AlphabeticalSongOrder(source, map);
            }
            throw new ArgumentException();
        }
    }

    public class DefinedSongOrder : SongOrder
    {
        private readonly List<SongPredicate> DefinedOrder;
        public DefinedSongOrder(IMusicItem source, YamlSequenceNode yaml) : base(source)
        {
            DefinedOrder = yaml.Children.Select(x => new SongPredicate((string)x)).ToList();
        }

        public override SongMetadata GetOrderMetadata(IMusicItem item)
        {
            for (int i = 0; i < DefinedOrder.Count; i++)
            {
                if (DefinedOrder[i].Matches(Source, item))
                    return new SongMetadataBuilder()
                    {
                        TrackNumber = MetadataProperty<uint>.Create((uint)i + 1),
                        TrackTotal = MetadataProperty<uint>.Create((uint)item.Parent.Songs.Count),
                    }.Build();
            }
            return new SongMetadataBuilder().Build();
        }
    }

    public class AlphabeticalSongOrder : SongOrder
    {
        public AlphabeticalSongOrder(IMusicItem source, YamlMappingNode yaml) : base(source)
        {
        }

        public override SongMetadata GetOrderMetadata(IMusicItem item)
        {
            var all = item.Parent.Songs.OrderBy(x => x.SimpleName).ToList();
            int index = all.IndexOf((Song)item);
            return new SongMetadataBuilder()
            {
                TrackNumber = MetadataProperty<uint>.Create((uint)index + 1),
                TrackTotal = MetadataProperty<uint>.Create((uint)all.Count)
            }.Build();
        }
    }

    public abstract class SongOrder
    {
        protected readonly IMusicItem Source;
        public SongOrder(IMusicItem source)
        {
            Source = source;
        }

        public abstract SongMetadata GetOrderMetadata(IMusicItem item);
    }
}
