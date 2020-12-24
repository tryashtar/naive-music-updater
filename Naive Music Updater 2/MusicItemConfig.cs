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
        public readonly List<SongPredicate> TrackOrder;
        public readonly List<(SongPredicate predicate, IMetadataStrategy strategy)> MetadataStrategies;
        public MusicItemConfig(IMusicItem item, string file)
        {
            Item = item;
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                var stream = new YamlStream();
                stream.Load(reader);
                var root = stream.Documents.SingleOrDefault()?.RootNode;
                TrackOrder = (root?.TryGet("order") as YamlSequenceNode)?.Children.Select(x => new SongPredicate((string)x)).ToList();
                MetadataStrategies = (root?.TryGet("set") as YamlMappingNode)?.Children.Select(x => ParseStrategy(x.Key, x.Value)).ToList();
            }
        }

        private (SongPredicate predicate, IMetadataStrategy strategy) ParseStrategy(YamlNode key, YamlNode value)
        {
            var predicate = new SongPredicate((string)key);
            var reference = value.TryGet("reference");
            IMetadataStrategy strategy;
            if (reference != null)
                strategy = Item.GlobalCache.Config.GetNamedStrategy((string)reference);
            else
                strategy = MetadataStrategyFactory.Create(value);
            return (predicate, strategy);
        }

        private SongMetadata GetOrderMetadata(IMusicItem item)
        {
            if (TrackOrder == null)
                return new SongMetadataBuilder().Build();
            for (int i = 0; i < TrackOrder.Count; i++)
            {
                if (TrackOrder[i].Matches(Item, item))
                    return new SongMetadataBuilder() { TrackNumber = MetadataProperty<uint>.Create((uint)i + 1) }.Build();
            }
            return new SongMetadataBuilder().Build();
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            var first = GetOrderMetadata(item);
            if (MetadataStrategies == null)
                return first;
            var metadata = new List<SongMetadata>();
            foreach (var (predicate, strategy) in MetadataStrategies)
            {
                if (predicate.Matches(Item, item))
                    metadata.Add(strategy.Perform(item));
            }
            if (!metadata.Any())
                return first;
            return first.Combine(SongMetadata.Merge(metadata));
        }
    }
}
