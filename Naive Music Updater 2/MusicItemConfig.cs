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
                    TrackOrder = new SongOrder(Item, order);
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

    public class SongOrder
    {
        private readonly Mode OrderMode;
        private readonly IMusicItem Source;
        private readonly List<SongPredicate> DefinedOrder;
        public SongOrder(IMusicItem source, YamlNode yaml)
        {
            Source = source;
            if (yaml is YamlSequenceNode sequence)
            {
                OrderMode = Mode.Defined;
                DefinedOrder = sequence.Children.Select(x => new SongPredicate((string)x)).ToList();
            }
            else if (yaml is YamlMappingNode map)
            {
                var mode = yaml.TryGet("mode");
                if ((string)mode == "alphabetical")
                    OrderMode = Mode.Alphabetical;
            }
            else
                throw new ArgumentException();
        }

        public SongMetadata GetOrderMetadata(IMusicItem item)
        {
            if (OrderMode == Mode.Defined)
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
            }
            else if (OrderMode == Mode.Alphabetical)
            {
                var all = item.Parent.Songs.OrderBy(x => x.SimpleName).ToList();
                int index = all.IndexOf((Song)item);
                return new SongMetadataBuilder()
                {
                    TrackNumber = MetadataProperty<uint>.Create((uint)index + 1),
                    TrackTotal = MetadataProperty<uint>.Create((uint)all.Count)
                }.Build();
            }
            return new SongMetadataBuilder().Build();
        }

        private enum Mode
        {
            Alphabetical,
            Defined
        }
    }
}