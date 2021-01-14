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
        public readonly SongOrder TrackOrder;
        public readonly IMetadataStrategy LocalMetadata;
        // have to defer these because some of the data needed isn't ready until after constructor is done
        public readonly List<Func<MusicFolder, (ItemSelector selector, IMetadataStrategy strategy)>> MetadataStrategies;
        public MusicItemConfig(string file)
        {
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                var stream = new YamlStream();
                stream.Load(reader);
                var root = stream.Documents.SingleOrDefault()?.RootNode;
                var order = root?.TryGet("order");
                if (order != null)
                    TrackOrder = SongOrderFactory.FromNode(order);
                var local = root?.TryGet("this");
                if (local != null)
                    LocalMetadata = MetadataStrategyFactory.Create(local);
                MetadataStrategies = (root?.TryGet("set") as YamlMappingNode)?.Children.Select(x => (Func<MusicFolder, (ItemSelector, IMetadataStrategy)>)(f => ParseStrategy(f, x.Key, x.Value))).ToList() ?? new List<Func<MusicFolder, (ItemSelector, IMetadataStrategy)>>();
            }
        }

        private (ItemSelector selector, IMetadataStrategy strategy) ParseStrategy(MusicFolder folder, YamlNode key, YamlNode value)
        {
            var selector = ItemSelector.FromNode(key);
            var reference = value.TryGet("reference");
            IMetadataStrategy strategy;
            if (reference != null)
                strategy = folder.GlobalCache.Config.GetNamedStrategy((string)reference);
            else
                strategy = MetadataStrategyFactory.Create(value);
            return (selector, strategy);
        }
    }

    public static class SongOrderFactory
    {
        public static SongOrder FromNode(YamlNode yaml)
        {
            if (yaml is YamlSequenceNode sequence)
                return new DefinedSongOrder(sequence);
            if (yaml is YamlMappingNode map)
                return new FolderSongOrder(map);
            throw new ArgumentException();
        }
    }

    public abstract class SongOrder
    {
        public abstract void ApplyAll(MusicFolder folder);
    }

    public class DefinedSongOrder : SongOrder
    {
        private readonly List<ItemSelector> DefinedOrder;
        public DefinedSongOrder(YamlSequenceNode yaml)
        {
            DefinedOrder = yaml.Children.Select(x => ItemSelector.FromNode(x)).ToList();
        }

        public override void ApplyAll(MusicFolder folder)
        {
            
        }
    }

    public class FolderSongOrder : SongOrder
    {
        private readonly SortType Sort;
        public FolderSongOrder(YamlMappingNode yaml)
        {
            var sort = yaml.TryGet("sort");
            if ((string)sort == "alphabetical")
                Sort = SortType.Alphabetical;
        }

        public override void ApplyAll(MusicFolder folder)
        {
            
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
