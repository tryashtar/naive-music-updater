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
        public List<IItemPredicate> order;
        public List<(IItemPredicate predicate, MetadataStrategy strategy)> set;
        public static MusicItemConfig ParseFile(string file)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(ConfigTypeConverter.Instance)
                .Build();
            return deserializer.Deserialize<MusicItemConfig>(File.ReadAllText(file));
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            var metadata = new List<SongMetadata>();
            foreach (var (predicate, strategy) in set)
            {
                if (predicate.Matches(item))
                    metadata.Add(strategy.Perform(item));
            }
            return SongMetadata.Merge(metadata);
        }
    }

    public class ConfigTypeConverter : IYamlTypeConverter
    {
        public static ConfigTypeConverter Instance => new ConfigTypeConverter();
        private ConfigTypeConverter() { }

        public bool Accepts(Type type)
        {
            return typeof(IItemPredicate).IsAssignableFrom(type);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;
            return ItemPredicateFactory.CreateFrom(value);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }
}
