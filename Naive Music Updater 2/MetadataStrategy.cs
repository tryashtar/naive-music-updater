using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Flac;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public enum MetadataFieldType
    {
        String,
        StringList,
        Number
    }

    public class MetadataField
    {
        public readonly string Name;
        public readonly MetadataFieldType Type;
        public readonly string[] Aliases;
        public Predicate<MetadataField> Only => x => x == this;
        public static Predicate<MetadataField> All => x => true;
        private static readonly Dictionary<string, MetadataField> AliasCache = new Dictionary<string, MetadataField>();
        private MetadataField(string name, MetadataFieldType type, params string[] aliases)
        {
            Name = name;
            Type = type;
            Aliases = aliases;
            foreach (var value in aliases)
            {
                AliasCache.Add(value, this);
            }
        }

        public static MetadataField FromID(string id)
        {
            if (AliasCache.TryGetValue(id, out var result))
                return result;
            return null;
        }

        public static MetadataField Title = new MetadataField("Title", MetadataFieldType.String, "title");
        public static MetadataField Album = new MetadataField("Album", MetadataFieldType.String, "album");
        public static MetadataField Performers = new MetadataField("Performers", MetadataFieldType.StringList, "performer", "performers");
        public static MetadataField AlbumArtists = new MetadataField("Album Artists", MetadataFieldType.StringList, "album artist", "album artists");
        public static MetadataField Composers = new MetadataField("Composers", MetadataFieldType.StringList, "composer", "composers");
        public static MetadataField Arranger = new MetadataField("Arranger", MetadataFieldType.String, "arranger");
        public static MetadataField Comment = new MetadataField("Comment", MetadataFieldType.String, "comment");
        public static MetadataField Track = new MetadataField("Track Number", MetadataFieldType.Number, "track");
        public static MetadataField TrackTotal = new MetadataField("Track Total", MetadataFieldType.Number, "track count", "track total");
        public static MetadataField Year = new MetadataField("Year", MetadataFieldType.Number, "year");
        public static MetadataField Language = new MetadataField("Language", MetadataFieldType.String, "lang", "language");
        public static MetadataField Genres = new MetadataField("Genres", MetadataFieldType.StringList, "genre", "genres");

        public static MetadataField[] Values;

        static MetadataField()
        {
            Values = new MetadataField[]
            {
                Title,
                Album,
                Performers,
                AlbumArtists,
                Composers,
                Arranger,
                Comment,
                Track,
                TrackTotal,
                Year,
                Language,
                Genres
            };
        }
    }

    public interface IMetadataStrategy
    {
        Metadata Get(IMusicItem item, Predicate<MetadataField> desired);
    }

    public static class MetadataStrategyFactory
    {
        public static IMetadataStrategy Create(YamlNode node)
        {
            if (node is YamlMappingNode map)
                return new MetadataStrategy(map);
            if (node is YamlSequenceNode list)
                return new MultipleMetadataStrategy(list);
            throw new ArgumentException();
        }
    }

    public class NoOpMetadataStrategy : IMetadataStrategy
    {
        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            return new Metadata();
        }
    }

    public class MetadataStrategy : IMetadataStrategy
    {
        private readonly Dictionary<MetadataField, MetadataSelector> Fields = new Dictionary<MetadataField, MetadataSelector>();
        
        public MetadataStrategy(YamlMappingNode yaml)
        {
            foreach (var pair in yaml)
            {
                var field = MetadataField.FromID((string)pair.Key);
                if (field != null)
                    Fields[field] = MetadataSelectorFactory.FromToken(pair.Value);
            }
        }

        private MetadataProperty Get(MetadataSelector selector, IMusicItem item)
        {
            return selector?.Get(item) ?? MetadataProperty.Ignore();
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, Get(pair.Value, item));
            }
            return meta;
        }
    }

    public class MultipleMetadataStrategy : IMetadataStrategy
    {
        private readonly List<IMetadataStrategy> Substrategies;

        public MultipleMetadataStrategy(YamlSequenceNode yaml)
        {
            Substrategies = new List<IMetadataStrategy>();
            foreach (var item in yaml.Children)
            {
                Substrategies.Add(MetadataStrategyFactory.Create(item));
            }
        }

        public MultipleMetadataStrategy(IEnumerable<IMetadataStrategy> strategies)
        {
            Substrategies = strategies.ToList();
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var datas = Substrategies.Select(x => x.Get(item, desired));
            return Metadata.FromMany(datas);
        }
    }
}
