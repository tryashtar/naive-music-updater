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
        public readonly IMetadataStrategy LocalMetadata;
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
                var local = root?.TryGet("this");
                if (local != null)
                    LocalMetadata = MetadataStrategyFactory.Create(local);
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
            if (LocalMetadata != null)
                metadata.Add(LocalMetadata.Perform(item));
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
                return new FolderSongOrder(source, map);
            throw new ArgumentException();
        }
    }

    public class DefinedSongOrder : SongOrder
    {
        private readonly List<SongPredicate> DefinedOrder;
        public DefinedSongOrder(IMusicItem source, YamlSequenceNode yaml) : base(source)
        {
            DefinedOrder = yaml.Children.Select(x => SongPredicate.FromNode(x)).ToList();
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

    public class FolderSongOrder : SongOrder
    {
        private readonly SortType Sort;
        public FolderSongOrder(IMusicItem source, YamlMappingNode yaml) : base(source)
        {
            var sort = yaml.TryGet("sort");
            if ((string)sort == "alphabetical")
                Sort = SortType.Alphabetical;
        }

        public override SongMetadata GetOrderMetadata(IMusicItem item)
        {
            var all = item.Parent.Songs.OrderBy(GetSort()).ToList();
            int index = all.IndexOf((Song)item);
            return new SongMetadataBuilder()
            {
                TrackNumber = MetadataProperty<uint>.Create((uint)index + 1),
                TrackTotal = MetadataProperty<uint>.Create((uint)all.Count)
            }.Build();
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
