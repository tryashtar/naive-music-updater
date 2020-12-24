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
        private IMusicItem _Item;
        public IMusicItem Item => _Item;
        public List<SongPredicate> order;
        public List<(SongPredicate predicate, MetadataStrategy strategy)> set;
        public static MusicItemConfig ParseFile(IMusicItem item, string file)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(ConfigTypeConverter.Instance)
                .Build();
            var config = deserializer.Deserialize<MusicItemConfig>(File.ReadAllText(file));
            config._Item = item;
            return config;
        }

        private SongMetadata GetOrderMetadata(IMusicItem item)
        {
            if (order == null)
                return new SongMetadataBuilder().Build();
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].Matches(Item, item))
                    return new SongMetadataBuilder() { TrackNumber = MetadataProperty<uint>.Create((uint)i + 1) }.Build();
            }
            return new SongMetadataBuilder().Build();
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            var first = GetOrderMetadata(item);
            if (set == null)
                return first;
            var metadata = new List<SongMetadata>();
            foreach (var (predicate, strategy) in set)
            {
                if (predicate.Matches(Item, item))
                    metadata.Add(strategy.Perform(item));
            }
            return first.Combine(SongMetadata.Merge(metadata));
        }
    }

    public class ConfigTypeConverter : IYamlTypeConverter
    {
        public static ConfigTypeConverter Instance => new ConfigTypeConverter();
        private ConfigTypeConverter() { }

        public bool Accepts(Type type)
        {
            return typeof(SongPredicate).IsAssignableFrom(type);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            if (typeof(SongPredicate).IsAssignableFrom(type))
            {
                var value = parser.Consume<Scalar>().Value;
                return new SongPredicate(value);
            }
            throw new ArgumentException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }
}
